using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    public class Player
    {
        #region Calculation Parameters

        public static readonly int AP_DIV = 200;
        public static readonly int LVL_MOD = 2170;

        public static readonly int DET_DIV = 17940;
        public static readonly float DIRECT_HIT_MODIFIER = 0.25f;
        public static readonly float CRITICAL_HIT_BASE_MODIFIER = 0.4f;
        public static readonly float CRITICAL_HIT_BASE_CHANCE = 0.05f;
        public static readonly float BASE_GCD_DELAY = 2.5f;

        public static readonly int BASE_SKILL_SPEED = 364;
        public static readonly int BASE_CRITICAL_HIT_RATING = 364;
        public static readonly int BASE_DETERMINATION = 292;
        public static readonly int BASE_DIRECT_HIT = 364;

        #endregion

        #region Stats

        public int CriticalHitRating
        { get; set; }

        public int DirectHitRating
        { get; set; }

        public int Speed
        { get; set; }

        public int Determination
        { get; set; }

        /// <summary>
        /// Strength/Dexterity/etc.
        /// </summary>
        public int AttackPower
        { get; set; }

        public int WeaponDamage
        { get; set; }

        public int AutoAttackDamage
        { get; set; }

        public float AutoAttackDelay
        { get; set; }

        #region Calculated Stats

        public float BaseCriticalHitChance
        { get { return (CriticalHitRating - Player.BASE_CRITICAL_HIT_RATING) / (float)(5 * Player.LVL_MOD) + Player.CRITICAL_HIT_BASE_CHANCE; } }

        public float BaseCriticalHitModifier
        { get { return (CriticalHitRating - Player.BASE_CRITICAL_HIT_RATING) / (float)(5 * Player.LVL_MOD) + Player.CRITICAL_HIT_BASE_MODIFIER; } }

        public float BaseDirectHitChance
        { get { return (DirectHitRating - Player.BASE_DIRECT_HIT) / 40f; } }

        private float SpeedDamageBonus
        {
            get
            {
                float baseGCD = CalculateBaseGCDDelay(BASE_GCD_DELAY);
                float speedBonus = MathHelper.Trunc((BASE_GCD_DELAY / baseGCD), 3);
                return speedBonus;
            }
        }

        private float DeterminationDamageBonus
        {
            get
            {
                return 1 + MathHelper.Trunc((Determination - BASE_DETERMINATION) / 166.67f / 100, 3); ;
            }
        }

        /// <summary>
        /// Calculates the chance-based damage modifier. Based on critical chance and modifier and direct hit chance and modifier, this
        /// flattens out the damage ouput from chance-based stats for easy simulation.
        /// </summary>
        public float FlattenedChanceBasedDamageModifier
        { get { return 1 + (BaseCriticalHitChance * BaseCriticalHitModifier * BaseDirectHitChance * Player.DIRECT_HIT_MODIFIER); } }

        #endregion

        #endregion

        public JobMechanics Job
        { get; private set; }

        public Player(JobMechanics job)
        {
            Job = job;

            CriticalHitRating = BASE_CRITICAL_HIT_RATING;
            DirectHitRating = BASE_DIRECT_HIT;
            Speed = BASE_SKILL_SPEED;
            Determination = BASE_DETERMINATION;
            AttackPower = 2000;
            WeaponDamage = 80;
            AutoAttackDamage = 80;
            AutoAttackDelay = 2.8f;
        }

        /// <summary>
        /// Calculate damage (before flattened chance-based damage modifier)
        /// </summary>
        /// <param name="potency">Potency of skill.</param>
        /// <returns></returns>
        public virtual float CalculateWeaponSkillDamage(PlayerAction action, SimulationState state)
        {
            float damage = CalculateBaseWeaponSkillDamage(action.DamagePotency) * DeterminationDamageBonus * GetFlattenedChanceBasedDamageModifier(state, action);

            return Job.ModifyWeaponSkillDamage(damage, state, action.Name);
        }


        private float GetFlattenedChanceBasedDamageModifier(SimulationState state, PlayerAction action = null)
        {
            float criticalHitChance = Math.Min(100, BaseCriticalHitChance + Job.ModifyCriticalHitChance(BaseCriticalHitChance, state, action));
            float criticalHitModifier = BaseCriticalHitModifier + Job.ModifyCriticalHitModifier(BaseCriticalHitModifier, state, action);
            float directHitChance = Math.Min(100, BaseDirectHitChance + Job.ModifyDirectHitChance(BaseDirectHitChance, state, action));
            float directHitModifier = DIRECT_HIT_MODIFIER + Job.ModifyDirectHitModifier(Player.DIRECT_HIT_MODIFIER, state, action);

            return 1 + (criticalHitChance * criticalHitModifier * directHitChance * directHitModifier);
        }

        /// <summary>
        /// Calculates the GCD delay. Should include any buff's effects here.
        /// </summary>
        public float GetGCDDelay(SimulationState state, float gcdDelay)
        {
            float baseGCD = CalculateBaseGCDDelay(gcdDelay);

            return Job.ModifyGCDDelay(baseGCD, state);
        }

        private float CalculateBaseWeaponSkillDamage(float potency)
        {
            return potency * (1 + MathHelper.Trunc((Determination - BASE_DETERMINATION) / 166.67f / 100, 3));
            //float potencyMultiplier = potency / 100f;
            //float wdModifier = WeaponDamage + Helper.Trunc(BASE_DETERMINATION * Job.JobMod / 1000f);
            //float attackPowerModifier = Helper.Trunc(AttackPower / (float)AP_DIV + (1 - (BASE_DETERMINATION / (float)AP_DIV)), 2);
            //float determinationModifier = Helper.Trunc(Helper.Round(1 + ((Determination - BASE_DETERMINATION) / (float)DET_DIV), 4), 3);

            //return (int)Helper.Trunc(Helper.Round(Helper.Trunc(potencyMultiplier * wdModifier * attackPowerModifier) * determinationModifier, 1));
        }

        /// <summary>
        /// Calculates the GCD delay based solely on Speed.
        /// </summary>
        /// <returns></returns>
        public float CalculateBaseGCDDelay(float gcdDelay)
        {
            return MathHelper.Trunc((1 - MathHelper.Trunc((Speed - BASE_SKILL_SPEED) / (LVL_MOD / 0.13f), 3)) * gcdDelay, 2);
        }

        /// <summary>
        /// Calculate the tick of a dot given potency and state.
        /// </summary>
        /// <param name="potency"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public virtual float CalculateDoTTickDamage(float potency, SimulationState state)
        {
            float damage = potency * DeterminationDamageBonus * SpeedDamageBonus * GetFlattenedChanceBasedDamageModifier(state);

            return Job.ModifyDoTDamage(damage, state);
        }

        /// <summary>
        /// Calculate the tick of Auto Attacks given state.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public virtual float CalculateAutoAttackDamage(SimulationState state)
        {
            float damage = Job.GetAutoAttackPotency(state) * DeterminationDamageBonus * SpeedDamageBonus * GetFlattenedChanceBasedDamageModifier(state);

            return Job.ModifyAutoAttackDamage(damage, state);
        }
    }
}
