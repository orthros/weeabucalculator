using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    public class SimulationState
    {
        public Player Player
        { get; private set; }

        public StatusEffectTracker Debuffs
        { get; private set; }

        public StatusEffectTracker Buffs
        { get; private set; }

        public StatusEffectTracker DoTs
        { get; private set; }

        public ActionTracker GCDActions
        { get; private set; }

        public ActionTracker AllActions
        { get; private set; }

        public CooldownTracker Cooldowns
        { get; private set; }

        public Dictionary<string, int> Resources
        { get; private set; }

        public PlayerAction LastComboAction
        { get; private set; }

        public float CurrentTime
        { get; private set; }

        public float GCD
        { get; private set; }

        public float AnimationLock
        { get; private set; }

        public DamageTracker Damage
        { get; private set; }

        public Log Log
        { get; private set; }

        #region Tree Properties

        public SimulationState PreviousStep
        { get; set; }

        public List<SimulationState> NextSteps
        { get; private set; }

        private int? _stepNumberCache;
        public int StepNumber
        {
            get
            {
                if (!_stepNumberCache.HasValue) _stepNumberCache = GetStepNumber();
                return _stepNumberCache.Value;
            }
        }

        public int GCDStep
        {
            get { return GCDActions.History.Count(); }
        }

        #endregion


        public SimulationState(Player player, SimulationState previousStep)
        {
            this.PreviousStep = previousStep;
            NextSteps = new List<SimulationState>();
            Player = player;
            Debuffs = new StatusEffectTracker(this, (state) => state.Debuffs);
            Buffs = new StatusEffectTracker(this, (state) => state.Buffs);
            DoTs = new StatusEffectTracker(this, (state) => state.DoTs);
            GCDActions = new ActionTracker(this, (state) => state.GCDActions);
            AllActions = new ActionTracker(this, (state) => state.AllActions);
            Cooldowns = new CooldownTracker(this);
            Resources = new Dictionary<string, int>();
            CurrentTime = 0;
            GCD = 0;
            AnimationLock = 0;
            Log = new Log(this);
            Damage = new DamageTracker(this);
        }

        /// <summary>
        /// Clones the current player state. This is a deep copy operation.
        /// </summary>
        /// <returns></returns>
        public SimulationState Clone(SimulationState previousStep)
        {
            SimulationState newState = new SimulationState(Player, previousStep);
            foreach (var resource in Resources)
            {
                newState.Resources.Add(resource.Key, resource.Value);
            }
            newState.LastComboAction = LastComboAction;
            newState.CurrentTime = CurrentTime;
            newState.GCD = GCD;
            newState.AnimationLock = AnimationLock;

            return newState;
        }

        /// <summary>
        /// Initializes the DoT snapshot.
        /// </summary>
        /// <param name="dot"></param>
        /// <returns></returns>
        public DoTInitialModifiers GetDoTInitialState(PlayerDoT dot)
        {
            // Precalculate the DoT tick
            float dotTick = Player.CalculateDoTTickDamage(dot.Potency, this);

            return new DoTInitialModifiers(dotTick);
        }

        /// <summary>
        /// Performs the given action, advancing time until it can perform the action, adding the buffs/debuffs/DoTs, dealing damage
        /// </summary>
        /// <param name="action"></param>
        public void PerformAction(PlayerAction action)
        {
            var comboAction = action;
            // Make sure we're using the modified action because we're in a combo
            if (action.HasComboActionProperties)
            {
                if ((LastComboAction != null && action.ComboAction.PreviousAction == LastComboAction.Name) || Player.Job.ShouldForceComboAction(this, action))
                {
                    comboAction = action.ComboAction.ActionModifier;
                }
                LastComboAction = null;
            }

            #region Advance Time

            // Prevent actions until animation lock is dealt with
            if (AnimationLock > 0)
                AdvanceTime(AnimationLock);

            if (comboAction.GCD)
            {
                // if our GCD is currently on CD, advance to when it's not.
                if (GCD != 0)
                    AdvanceTime(GCD);
            }

            if (comboAction.Cooldown > 0)
            {
                // we're not on the GCD. Check the CD remaining and advance that amount.
                float remainingCD = Cooldowns.GetRemainingTime(comboAction.CooldownID, comboAction.Cooldown);
                if (remainingCD > 0)
                    AdvanceTime(remainingCD);
            }

            if (comboAction.CastTime > 0)
            {
                Log.Write(CurrentTime, $"Begin casting {action.Name}");
                AdvanceTime(comboAction.CastTime);
            }

            #endregion

            #region Perform Action

            Log.Write(CurrentTime, $"Performing {action.Name}");

            // Start animation lock and GCD timers.
            AnimationLock = comboAction.AnimationLock;
            if (comboAction.GCD) GCD = Player.GetGCDDelay(this, comboAction.GCDDelay);

            float damage = 0;
            // Deal damage if action has damage
            if (comboAction.DamagePotency > 0)
            {
                damage = Player.CalculateWeaponSkillDamage(comboAction, this);
                Damage.AddDamage(damage, CurrentTime);
                Log.Write(CurrentTime, $"{action.Name} deals {damage} damage");
            }

            PlayerDoT dot = null;
            // Initialize and apply a DoT if there is one
            if (comboAction.DoT != null)
            {
                dot = comboAction.DoT.Clone();
                dot.InitializeDoT(this);
                DoTs.ActivateEffect(dot);
            }

            // Add the buffs applied by this action
            foreach (var buff in comboAction.Buffs)
            {
                Buffs.ActivateEffect(buff);
            }

            // Add the debuffs applied by this action
            foreach (var debuff in comboAction.Debuffs)
            {
                Debuffs.ActivateEffect(debuff);
            }

            if (comboAction.Cooldown > 0)
            {
                // Start the CD.
                Cooldowns.StartCooldown(comboAction.CooldownID, comboAction.Cooldown);
            }

            // Add resources this action accumulates
            foreach (var resourceChange in comboAction.ResourceChanges)
            {
                ChangeResource(resourceChange.ResourceName, resourceChange.Change);
            }

            #endregion

            // Record actions
            AllActions.RecordActionPerformed(action, damage, dot);
            if (comboAction.GCD) GCDActions.RecordActionPerformed(action, damage, dot);
            if (comboAction.IsComboInitiator) LastComboAction = action;

            Player.Job.OnActionPerformed(action, this);
        }

        /// <summary>
        /// Advances time, performing DoT and AA ticks, advancing Buffs/Debuffs/DoT timers, updating GCD and AnimationLock times,
        /// and advancing CurrentTime
        /// </summary>
        /// <param name="time"></param>
        private void AdvanceTime(float time)
        {
            // Tick DoTs and AA first
            var numDoTTicksBefore = MathHelper.Trunc(CurrentTime / 3f);
            var numDoTTicksAfter = MathHelper.Trunc((CurrentTime + time) / 3f);
            var numDotTicks = numDoTTicksAfter - numDoTTicksBefore;

            // for each DoT tick until CurrentTime + time, tick once for each active DoT and once for each AA.
            for (var i = 1; i <= numDotTicks; i++)
            {
                var nextDoTTime = (numDoTTicksBefore + i) * 3;
                var timeUntilNextDot = nextDoTTime - CurrentTime;
                foreach (PlayerDoT dot in DoTs.GetActiveEffects(60))
                {
                    // Make sure the DoT will be active when this tick comes up.
                    if (DoTs.GetRemainingTime(dot) > timeUntilNextDot)
                    {
                        var damage = dot.GetDoTTick(this);
                        Damage.AddDamage(damage, nextDoTTime);
                        Log.Write(nextDoTTime, $"{dot.EffectID} ticks for {damage}");
                    }
                }

                // Add AA damage.
                var aaDamage = Player.CalculateAutoAttackDamage(this);
                Damage.AddDamage(aaDamage, nextDoTTime);
                Log.Write(nextDoTTime, $"Auto Attack for {aaDamage}");
            }

            // Advance our buffs
            foreach (var buff in Buffs.GetActiveEffects(60))
            {
                if (!buff.Duration.HasValue) continue;
                var remainingTime = Buffs.GetRemainingTime(buff).Value - time;
                if (remainingTime <= 0)
                {
                    Buffs.EndEffect(buff);
                }
            }

            // Advance our debuffs
            foreach (var debuff in Debuffs.GetActiveEffects(60))
            {
                if (!debuff.Duration.HasValue) continue;
                var remainingTime = Debuffs.GetRemainingTime(debuff).Value - time;
                if (remainingTime <= 0)
                {
                    Debuffs.EndEffect(debuff);
                }
            }

            // Advance our DoTs
            foreach (var dot in DoTs.GetActiveEffects(60))
            {
                if (!dot.Duration.HasValue) continue;
                var remainingTime = DoTs.GetRemainingTime(dot).Value - time;
                if (remainingTime <= 0)
                {
                    DoTs.EndEffect(dot);
                }
            }

            // Advance the GCD if it has any time left.
            GCD = Math.Max(0, GCD - time);
            // Advance the animation lock if it has any time left.
            AnimationLock = Math.Max(0, AnimationLock - time);

            // Finally, advance our current time.
            CurrentTime += time;
        }

        public void ChangeResource(string resourceName, int amount, bool relative = true)
        {
            if (!Resources.ContainsKey(resourceName)) Resources[resourceName] = 0;

            if (relative)
            {
                Resources[resourceName] += amount;
                Resources[resourceName] = Player.Job.CorrectResourceRange(resourceName, Resources[resourceName]);
                if (amount > 0) Log.Write(CurrentTime, $"Added {amount} {resourceName}, new value: {Resource(resourceName)}");
                else if (amount < 0) Log.Write(CurrentTime, $"Removed {Math.Abs(amount)} {resourceName}, new value: {Resource(resourceName)}");
            }
            else
            {
                Resources[resourceName] = amount;
                Resources[resourceName] = Player.Job.CorrectResourceRange(resourceName, Resources[resourceName]);
                Log.Write(CurrentTime, $"Set {resourceName} = {amount}");
            }
            Player.Job.OnResourceChanged(resourceName, this);
        }

        public int Resource(string resourceName)
        {
            if (!Resources.ContainsKey(resourceName)) return 0;

            return Resources[resourceName];
        }

        public void ClearLastComboAction()
        {
            LastComboAction = null;
        }

        #region Tree Helpers

        private int GetStepNumber(SimulationState step = null, int stepCount = 1)
        {
            if (step == null) step = this;

            if (step.PreviousStep == null) return stepCount;

            return GetStepNumber(step.PreviousStep, stepCount + 1);
        }

        public IEnumerable<SimulationState> PerformActions(IEnumerable<PlayerAction> actions)
        {
            NextSteps.Clear();
            foreach (PlayerAction action in actions)
            {
                var state = this.Clone(this);
                state.PerformAction(action);
                NextSteps.Add(state);
            }

            return NextSteps;
        }

        #endregion

    }
}
