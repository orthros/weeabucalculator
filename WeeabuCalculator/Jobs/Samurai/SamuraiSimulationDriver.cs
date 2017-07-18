using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    [DeepSimulationDriver("SamuraiRotationSimulation")]
    public class SamuraiSimulationDriver : DeepSimulationDriver
    {
        public static float BUFF_WINDOW_MAX = 24;
        public static float BUFF_WINDOW_MIN = 12;

        private PlayerAction[] SpecialHagakure;

        public float EndTime { get; private set; }
        private SamuraiOpenerSimulationDriver _openerDriver;

        public SamuraiSimulationDriver(JobMechanics job) : base(job)
        {
            EndTime = 300;
            TopSimulationsToKeep = 100;
            // Shinten damage + 1 Guren per 3 hagakures
            var hagakureOpportunityDamage = 300 / 25 + ((800 / 50) / 3);
            SpecialHagakure = new PlayerAction[3];
            SpecialHagakure[0] = new PlayerAction("Special Hagakure")
                .HasCooldown(40, "Hagakure")
                .HasAnimationLock(0.5f)
                .AppliesDoT(new PlayerDoT("Hagakure", (20 * hagakureOpportunityDamage) / 13f, 39));
            SpecialHagakure[1] = new PlayerAction("Special Hagakure")
                .HasCooldown(40, "Hagakure")
                .HasAnimationLock(0.5f)
                .AppliesDoT(new PlayerDoT("Hagakure", (40 * hagakureOpportunityDamage) / 13f, 39));
            SpecialHagakure[2] = new PlayerAction("Special Hagakure")
                .HasCooldown(40, "Hagakure")
                .HasAnimationLock(0.5f)
                .AppliesDoT(new PlayerDoT("Hagakure", (60 * hagakureOpportunityDamage) / 13f, 39));

            _openerDriver = new SamuraiOpenerSimulationDriver(job);
        }

        public override (ResultState state, float score) GetInitialResultScore(SimulationState result)
        {
            return _openerDriver.GetResultScore(result);
        }

        public override (ResultState state, float score) GetResultScore(SimulationState result)
        {
            if (result.CurrentTime < BUFF_WINDOW_MAX) return (ResultState.Inconclusive, 0);

            // The opener score should prioritize as much damage between 12 and 24s (for buff window)
            float buffWindowDamage = 0;
            foreach (var a in result.AllActions.GetActionsBetween(BUFF_WINDOW_MIN, BUFF_WINDOW_MAX))
            {
                buffWindowDamage += a.Damage;

                // action applies a DoT, include it's total snapshoted damage.
                if (a.DoT != null)
                {
                    // loss due to waiting to put this up.
                    var opportunityLoss = ((a.Time - BUFF_WINDOW_MIN) / 3f) * a.DoT.InitialState.Value.DamageOnTick;
                    buffWindowDamage += a.DoT.InitialState.Value.DamageOnTick * (a.DoT.Duration.Value / 3f) - opportunityLoss;
                }
            }

            var buffWindowDPS = (buffWindowDamage / (result.CurrentTime - BUFF_WINDOW_MIN));

            // include all other DPS but count the buff window more, and subtract the opportunity cost from CDs.
            var score = result.Damage.DPS + buffWindowDPS * 0.15f;

            return (result.CurrentTime >= EndTime ? ResultState.Conclusive : ResultState.Inconclusive, score);
        }

        private bool OpenerBuffsApplied(SimulationState state)
        {
            return Job.HasBuff(state, "Jinpu") && Job.HasBuff(state, "Shifu") && Job.HasDebuff(state, "Slashing Resistance Down");
        }

        public override IEnumerable<PlayerAction> GetActionSuggestions(SimulationState state)
        {
            var senCount = GetSenCount(state);
            var kaitenUp = Job.HasBuff(state, "Hissatsu: Kaiten");

            // Only use Iaijutsu when Kaiten is up.
            if (kaitenUp)
            {
                if (senCount == 3)
                {
                    yield return Job.Actions["Midare Setsugekka"];

                    yield break; // Force Midare when Kaiten is up. Prevents a bunch of dumb paths.
                }
                else if (senCount == 1)
                {
                    yield return Job.Actions["Higanbana"];

                    yield break; // Force Iaijutsu when Kaiten is up. Prevents a bunch of dumb paths.
                }
            }

            var kenki = state.Resource("Kenki");

            if (state.GCD > 0 && senCount > 1 && kenki <= (100 - (senCount * 20)) &&
                !state.Cooldowns.IsCooldownRunning("Hagakure") && (senCount == 3 || TimeUntilNextSen(state) > 5))
            {
                yield return SpecialHagakure[senCount - 1];
                yield break;
            }

            // Kenki spenders
            if (state.GCD > 0 && kenki >= 45)
            {
                yield return Job.Actions["Hissatsu: Shinten"];
                yield break; // force a shinten to prevent paths holding onto it. 
            }

            //if (state.GCD > 0 && kenki > 50 && !Job.OnCooldown(state, "Hissatsu: Guren", 118))
            //    yield return Job.Actions["Hissatsu: Guren"];

            if (state.GCD > 0 && !kaitenUp && kenki > 20 &&
                ((senCount == 3 && Job.GetRemainingCooldownTime(state, "Hagakure", 40) > TimeUntilNextSen(state)) ||
                 (senCount == 1 && (Job.GetRemainingDoTTime(state, "Higanbana") < 3 || (NumGCDUntilNextSen(state) == 0 && Job.GetRemainingDoTTime(state, "Higanbana") < 8)))))
            {
                yield return Job.Actions["Hissatsu: Kaiten"];
                yield break;
            }

            // Meikyo Shisui. 
            //if (state.GCD > 0 && senCount < 3 && !Job.OnCooldown(state, "Meikyo Shisui", 80))
            if (senCount < 3 && !Job.OnCooldown(state, "Meikyo Shisui", 80))
            {
                yield return Job.Actions["Meikyo Shisui"];
                yield break;
            }

            // Combo actions
            if (Job.HasBuff(state, "Meikyo Shisui"))
            {
                var nextMeikyoAction = GetNextMeikyoFinisher(state);
                if (nextMeikyoAction != null) yield return Job.Actions[nextMeikyoAction];
            }
            else
            {
                if (Job.LastComboActionWas(state, "Hakaze"))
                {
                    yield return Job.Actions[GetNextComboForkItem(state)];
                }
                else if (Job.LastComboActionWas(state, "Jinpu"))
                    yield return Job.Actions["Gekko"];
                else if (Job.LastComboActionWas(state, "Shifu"))
                    yield return Job.Actions["Kasha"];
                else
                    yield return Job.Actions["Hakaze"];
            }
        }

        private bool IsAboutToUseSenSpender(SimulationState state)
        {
            return Job.GetRemainingCooldownTime(state, "Hagakure", 40) < 3 ||
                   (state.Resource("Kenki") >= 15 && GetSenCount(state) != 2);
        }

        private float ShifuWeight(SimulationState state)
        {
            return (30 - Job.GetRemainingBuffTime(state, "Shifu") ?? 0);
        }

        private float JinpuWeight(SimulationState state)
        {
            return (30 - Job.GetRemainingBuffTime(state, "Jinpu") ?? 0);
        }

        private int GetSenCount(SimulationState state)
        {
            return ((SamuraiJobMechanics)Job).GetSenCount(state);
        }

        private bool IsSenActive(SimulationState state, string senName)
        {
            return state.Resource(senName) == 1;
        }

        private bool IsHiganbanaReady(SimulationState state, int senCount)
        {
            return (senCount == 1 && (!Job.HasDoT(state, "Higanbana") || Job.GetRemainingDoTTime(state, "Higanbana") < 15));
        }

        private int NumGCDUntilNextSen(SimulationState state)
        {
            if (Job.HasBuff(state, "Meikyo Shisui")) return 0;

            var nextForkItem = GetNextComboForkItem(state);
            var previousCombo = state.LastComboAction?.Name;
            switch (nextForkItem)
            {
                case "Jinpu":
                case "Shifu":
                    if (previousCombo == null) return 2; // Just finished a combo, Hakaze -> Jinpu/Shifu -> Sen.
                    else if (previousCombo == "Hakaze") return 1; // We're about to use Jinpu/Shifu. The action after that will generate Sen.
                    else return 0; // We just finished Jinpu/Shifu, Sen generation is immenant!
                case "Yukikaze":
                    if (previousCombo == null) return 1; // Just finished a combo.
                    else if (previousCombo == "Hakaze") return 0; // We're about to use Yukikaze, Sen generation is immenant!
                    else return 0; // We JUST used a fork combo.
            }
            throw new InvalidOperationException("NextForkItem returned an unexpected result");
        }

        private float TimeUntilNextSen(SimulationState state)
        {
            var numGCD = NumGCDUntilNextSen(state);
            return state.GCD + (numGCD * state.Player.GetGCDDelay(state, 2.5f));
        }

        private static string[] forks = new string[] { "Jinpu", "Shifu", "Yukikaze" };
        private string GetNextComboForkItem(SimulationState state)
        {
            var orderedForks = (from f in forks
                                let e = GetEffectForComboFork(f)
                                let t = (f != "Yukikaze") ? Job.GetRemainingBuffTime(state, e) : Job.GetRemainingDebuffTime(state, e)
                                orderby t ascending
                                select f);
            if (GetSenCount(state) == 3)
            {
                return orderedForks.First();
            }
            else
            {
                foreach (var f in orderedForks)
                {
                    if (!IsSenActive(state, GetSenForComboFork(f))) return f;
                }
            }
            throw new InvalidOperationException("Fork combo action not handled");
        }

        private string GetNextMeikyoFinisher(SimulationState state)
        {
            var orderedForks = (from f in forks
                                where !IsSenActive(state, GetSenForComboFork(f))
                                let e = GetEffectForComboFork(f)
                                let t = (f != "Yukikaze") ? Job.GetRemainingBuffTime(state, e) : 30 - Job.GetRemainingDebuffTime(state, e)
                                orderby t descending
                                select f);
            if (orderedForks.Any()) return GetSenForComboFork(orderedForks.First());

            return "Yukikaze";
        }

        private string GetSenForComboFork(string forkName)
        {
            switch (forkName)
            {
                case "Jinpu":
                    return "Gekko";
                case "Shifu":
                    return "Kasha";
                case "Yukikaze":
                    return "Yukikaze";
            }
            throw new InvalidOperationException("Unknown fork name");
        }

        private string GetEffectForComboFork(string forkName)
        {
            switch (forkName)
            {
                case "Jinpu":
                    return "Jinpu";
                case "Shifu":
                    return "Shifu";
                case "Yukikaze":
                    return "Slashing Resistance Down";
            }
            throw new InvalidOperationException("Unknown fork name");
        }

        public override IEnumerable<(ResultState state, float score, SimulationState step)> GenerateInitialStates(SimulationState root)
        {
            var sim = new DeepSimulator(root.Player, new SamuraiOpenerSimulationDriver(root.Player.Job), Console.Out, root);

            DateTime startTime = DateTime.Now;

            sim.FoundTopPerformer += (s, e) =>
            {
                Console.WriteLine($"{(DateTime.Now - startTime):hh\\:mm\\:ss} :: New top openner found! score: {e.score}");
            };

            var timerStep = 30f;
            var historyLength = 10;
            var t = new System.Timers.Timer(timerStep * 1000);
            var progressHistory = new Queue<(long progress, DateTime time)>(historyLength);
            t.Elapsed += (o, e) =>
            {
                Program.AnnounceProgress(sim, ref progressHistory, historyLength, startTime);
            };
            t.Start();

            sim.RunSimulation();
            t.Stop();

            var leavesScores = (from l in DeepSimulator.GetLeaves(root)
                                let r = _openerDriver.GetResultScore(l)
                                where r.state == ResultState.Conclusive
                                orderby r.score descending
                                select (r.state, r.score, l));
            foreach (var l in leavesScores.Take(100))
            {
                yield return l;
            }
        }

        public override void HandleArguments(string[] args)
        {
            if (args.Length == 0) EndTime = 300;
            else
            {
                EndTime = float.Parse(args[0]);
                TopSimulationsToKeep = int.Parse(args[1]);
            }

        }
    }
}
