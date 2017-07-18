using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    [JobMechanics("Samurai")]
    public class SamuraiJobMechanics : JobMechanics
    {

        public SamuraiJobMechanics() : base(110)
        { }

        protected override void InitializeActionsAndEffects()
        {

            AddAction("Hakaze")
                .DealsDamage(150)
                .IsGCD()
                .InitiatesCombo()
                .HasAnimationLock(0.75f)
                .AddsResource("Kenki", 5);
            AddAction("Jinpu")
                .DealsDamage(100)
                .IsGCD()
                .HasAnimationLock(0.75f)
                .IsComboAction("Hakaze",
                    new PlayerAction("Jinpu")
                        .DealsDamage(280)
                        .IsGCD()
                        .InitiatesCombo()
                        .HasAnimationLock(0.75f)
                        .AppliesBuff(AddEffect("Jinpu", 30))
                        .AddsResource("Kenki", 5));
            AddAction("Gekko")
                .DealsDamage(100)
                .IsGCD()
                .HasAnimationLock(0.75f)
                .IsComboAction("Jinpu",
                    new PlayerAction("Gekko")
                        .DealsDamage(400)
                        .IsGCD()
                        .HasAnimationLock(0.75f)
                        .AddsResource("Kenki", 10)
                        .AddsResource("Gekko", 1));

            AddAction("Shifu")
                .DealsDamage(100)
                .IsGCD()
                .HasAnimationLock(0.75f)
                .IsComboAction("Hakaze",
                    new PlayerAction("Shifu")
                        .DealsDamage(280)
                        .IsGCD()
                        .InitiatesCombo()
                        .HasAnimationLock(0.75f)
                        .AppliesBuff(AddEffect("Shifu", 30))
                        .AddsResource("Kenki", 5));
            AddAction("Kasha")
                .DealsDamage(100)
                .IsGCD()
                .HasAnimationLock(0.75f)
                .IsComboAction("Shifu",
                    new PlayerAction("Kasha")
                        .DealsDamage(400)
                        .IsGCD()
                        .HasAnimationLock(0.75f)
                        .AddsResource("Kenki", 10)
                        .AddsResource("Kasha", 1));

            AddAction("Yukikaze")
                .DealsDamage(100)
                .IsGCD()
                .HasAnimationLock(0.75f)
                .IsComboAction("Hakaze",
                    new PlayerAction("Yukikaze")
                        .DealsDamage(340)
                        .IsGCD()
                        .HasAnimationLock(0.75f)
                        .AppliesDebuff(AddEffect("Slashing Resistance Down", 30))
                        .AddsResource("Kenki", 10)
                        .AddsResource("Yukikaze", 1));

            AddAction("Midare Setsugekka")
                .DealsDamage(720)
                .HasCastTime(1.72f)
                .IsGCD()
                .HasAnimationLock(0.25f);

            AddAction("Higanbana")
                .DealsDamage(240)
                .HasCastTime(1.72f)
                .IsGCD()
                .HasAnimationLock(0.25f)
                .AppliesDoT(AddDoT("Higanbana", 35, 60));

            AddAction("Hagakure")
                .HasCooldown(40)
                .HasAnimationLock(0.5f);

            AddAction("Hissatsu: Shinten")
                .HasCooldown(1)
                .DealsDamage(300)
                .ConsumesResource("Kenki", 25)
                .HasAnimationLock(0.5f);

            AddAction("Hissatsu: Kaiten")
                .HasCooldown(5)
                .ConsumesResource("Kenki", 20)
                .AppliesBuff(AddEffect("Hissatsu: Kaiten", 10))
                .HasAnimationLock(0.75f);

            AddAction("Meikyo Shisui")
                .HasCooldown(80)
                .AppliesBuff(AddEffect("Meikyo Shisui", 10))
                .HasAnimationLock(0.5f);

            AddAction("Hissatsu: Guren")
                .HasCooldown(120)
                .DealsDamage(800)
                .ConsumesResource("Kenki", 50)
                .HasAnimationLock(0.5f);
        }


        public override float ModifyWeaponSkillDamage(float baseDamage, SimulationState state, string actionName = "")
        {
            PlayerAction action = null;
            if (!string.IsNullOrEmpty(actionName)) action = Actions[actionName];

            if (HasBuff(state, "Jinpu"))
                baseDamage = CalculateIntermediateWeaponSkillDamage(baseDamage, 1.1f);
            if (HasDebuff(state, "Slashing Resistance Down"))
                baseDamage = CalculateIntermediateWeaponSkillDamage(baseDamage, 1.1f);

            if (HasBuff(state, "Hissatsu: Kaiten") && action != null && action.GCD)
                baseDamage = CalculateIntermediateWeaponSkillDamage(baseDamage, 1.5f);

            return baseDamage;
        }

        public override float ModifyDoTDamage(float baseDamage, SimulationState state)
        {
            return ModifyWeaponSkillDamage(baseDamage, state);
        }

        public override float ModifyAutoAttackDamage(float baseDamage, SimulationState state)
        {
            return ModifyWeaponSkillDamage(baseDamage, state);
        }

        public override float ModifyGCDDelay(float baseDelay, SimulationState state)
        {
            if (HasBuff(state, "Shifu"))
            {
                return MathHelper.Trunc(baseDelay * 0.9f, 2);
            }
            return baseDelay;
        }

        public override void OnActionPerformed(PlayerAction action, SimulationState state)
        {
            if (HasBuff(state, "Hissatsu: Kaiten") && action.GCD)
                EndBuffEarly(state, "Hissatsu: Kaiten");

            if (action.Name == "Hagakure")
            {
                state.ChangeResource("Kenki", GetSenCount(state) * 20);
            }

            if (action.Name == "Midare Setsugekka" || action.Name == "Tenka Goken" || action.Name == "Higanbana" || action.Name == "Hagakure")
            {
                state.ChangeResource("Gekko", 0, false);
                state.ChangeResource("Kasha", 0, false);
                state.ChangeResource("Yukikaze", 0, false);
            }

            if (HasBuff(state, "Meikyo Shisui") && (action.Name == "Jinpu" || action.Name == "Shifu" || action.Name == "Yukikaze" || action.Name == "Gekko" || action.Name == "Kasha" || action.Name == "Hakaze"))
            {
                state.ChangeResource("Meikyo Shisui", -1);
                state.ClearLastComboAction();
            }

            base.OnActionPerformed(action, state);
        }

        public override void OnEffectEvent(StatusEffect effect, StatusEffectEventType type, SimulationState state)
        {
            if (effect == Effects["Meikyo Shisui"])
            {
                if (type == StatusEffectEventType.Ended) state.ChangeResource("Meikyo Shisui", 0, false);
                else state.ChangeResource("Meikyo Shisui", 3, false);
            }

            base.OnEffectEvent(effect, type, state);
        }

        public override void OnResourceChanged(string resourceName, SimulationState state)
        {
            if(resourceName == "Meikyo Shisui" && state.Resource(resourceName) <= 0)
                EndBuffEarly(state, "Meikyo Shisui");

            base.OnResourceChanged(resourceName, state);
        }

        public override int CorrectResourceRange(string resourceName, int currentValue)
        {
            if (resourceName == "Kenki")
                return Math.Max(Math.Min(currentValue, 100), 0);

            if (resourceName == "Gekko" || resourceName == "Kasha" || resourceName == "Yukikaze")
                return Math.Max(Math.Min(currentValue, 1), 0);

            return base.CorrectResourceRange(resourceName, currentValue);
        }

        public override bool ShouldForceComboAction(SimulationState state, PlayerAction action)
        {
            return HasBuff(state, "Meikyo Shisui");
        }

        public int GetSenCount(SimulationState state)
        {
            return state.Resource("Gekko") + state.Resource("Kasha") + state.Resource("Yukikaze");
        }
    }

}
