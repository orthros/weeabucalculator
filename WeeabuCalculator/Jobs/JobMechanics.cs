using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    public abstract class JobMechanics
    {
        public Dictionary<string, PlayerAction> Actions
        { get; private set; }
        public Dictionary<string, StatusEffect> Effects
        { get; private set; }

        /// <summary>
        /// Job_Mod value for damage/critical chance calculations.
        /// </summary>
        public int JobMod
        { get; private set; }

        protected JobMechanics(int jobMod)
        {
            JobMod = jobMod;
            Actions = new Dictionary<string, PlayerAction>();
            Effects = new Dictionary<string, StatusEffect>();

            InitializeActionsAndEffects();
        }

        /// <summary>
        /// Fill Actions and Effects with objects available to simulation.
        /// </summary>
        protected abstract void InitializeActionsAndEffects();

        /// <summary>
        /// Creates an action with the name specified and returns it for further modification.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected PlayerAction AddAction(string name)
        {
            Actions[name] = new PlayerAction(name);
            return Actions[name];
        }

        /// <summary>
        /// Creates an effect with the name specified and a duration and returns it for further modification.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        protected StatusEffect AddEffect(string name, float? duration = null)
        {
            Effects[name] = new StatusEffect(name, duration);
            return Effects[name];
        }

        /// <summary>
        /// Creates a dot with the name, potency and duration specified and returns it for further modification.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="potency"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        protected PlayerDoT AddDoT(string name, int potency, float duration)
        {
            var dot = new PlayerDoT(name, potency, duration);
            Effects[name] = dot;
            return dot;
        }

        /// <summary>
        /// Gets the basic Auto Attack potency.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public virtual int GetAutoAttackPotency(SimulationState state)
        {
            return 110; // for "Attack", "Shot" would be 100
        }

        /// <summary>
        /// Called when an action has finished being performed
        /// </summary>
        /// <param name="action"></param>
        /// <param name="state"></param>
        public virtual void OnActionPerformed(PlayerAction action, SimulationState state)
        { }

        /// <summary>
        /// Called when a status effect event happens such as when the effect begins or ends.
        /// </summary>
        public virtual void OnEffectEvent(StatusEffect effect, StatusEffectEventType type, SimulationState state)
        { }

        /// <summary>
        /// Called when resources change.
        /// </summary>
        public virtual void OnResourceChanged(string resourceName, SimulationState state)
        { }

        /// <summary>
        /// Modifies damage based on player buffs
        /// </summary>
        /// <param name="baseDamage">Base damage calculated based on stats</param>
        /// <param name="player">The player object that is dealing damage</param>
        /// <returns></returns>
        public virtual float ModifyWeaponSkillDamage(float baseDamage, SimulationState player, string actionName = "")
        {
            return baseDamage;
        }

        /// <summary>
        /// Calculates intermediate step damage. Use this function for each damage modifier.
        /// </summary>
        /// <param name="baseDamage">Base damage calculated based on stats</param>
        /// <param name="modifier">Damage multiplier from this buff</param>
        /// <returns></returns>
        protected float CalculateIntermediateWeaponSkillDamage(float baseDamage, float modifier)
        {
            return baseDamage * modifier; //(int)Helper.Trunc(baseDamage * modifier);
        }

        /// <summary>
        /// Modify DoT damage based on buffs.
        /// </summary>
        /// <param name="damage"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public virtual float ModifyDoTDamage(float baseDamage, SimulationState state)
        {
            return baseDamage;
        }

        /// <summary>
        /// Modify Auto Attack damage based on buffs.
        /// </summary>
        /// <param name="damage"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public virtual float ModifyAutoAttackDamage(float baseDamage, SimulationState state)
        {
            return baseDamage;
        }

        public virtual int CorrectResourceRange(string resourceName, int currentValue)
        { return currentValue; }

        /// <summary>
        /// Modifies GCD delay based on player buffs
        /// </summary>
        /// <param name="baseDelay">Base delay calculated based on stats</param>
        /// <param name="player">The player object that is performing an action</param>
        /// <returns></returns>
        public virtual float ModifyGCDDelay(float baseDelay, SimulationState player)
        {
            return baseDelay;
        }

        /// <summary>
        /// Should return true if combo actions should happen regardless of last action.
        /// E.g. - Perfect Balance or Meikyo Shisui
        /// </summary>
        /// <param name="state"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public virtual bool ShouldForceComboAction(SimulationState state, PlayerAction action)
        {
            return false;
        }

        /// <summary>
        /// Modify the critical hit chance of a particular action and based on buffs.
        /// </summary>
        /// <param name="baseValue"></param>
        /// <param name="state"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public virtual float ModifyCriticalHitChance(float baseValue, SimulationState state, PlayerAction action = null)
        {
            return baseValue;
        }

        /// <summary>
        /// Modify critical hit modifier of a particular action and based on buffs.
        /// </summary>
        /// <param name="baseValue"></param>
        /// <param name="state"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public virtual float ModifyCriticalHitModifier(float baseValue, SimulationState state, PlayerAction action = null)
        {
            return baseValue;
        }

        /// <summary>
        /// Modify the direct hit chance of a particular action and based on buffs.
        /// </summary>
        /// <param name="baseValue"></param>
        /// <param name="state"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public virtual float ModifyDirectHitChance(float baseValue, SimulationState state, PlayerAction action = null)
        {
            return baseValue;
        }

        /// <summary>
        /// Modify the direct hit modifier of a particular action and based on buffs.
        /// </summary>
        /// <param name="baseValue"></param>
        /// <param name="state"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public virtual float ModifyDirectHitModifier(float baseValue, SimulationState state, PlayerAction action = null)
        {
            return baseValue;
        }

        public virtual IEnumerable<PlayerAction> ReadResult(string result)
        {
            var tokens = result.Split('>');
            foreach(var token in tokens)
            {
                var cleanToken = token.Trim(" []".ToCharArray());
                if (Actions.ContainsKey(cleanToken)) yield return Actions[cleanToken];
            }
        }
        
        #region Helpers

        public bool HasBuff(SimulationState state, string name)
        {
            return state.Buffs.IsEffectActive(Effects[name]);
        }
        public float? GetRemainingBuffTime(SimulationState state, string name)
        {
            return state.Buffs.GetRemainingTime(Effects[name]);
        }
        public bool HasDebuff(SimulationState state, string name)
        {
            return state.Debuffs.IsEffectActive(Effects[name]);
        }
        public float? GetRemainingDebuffTime(SimulationState state, string name)
        {
            return state.Debuffs.GetRemainingTime(Effects[name]);
        }
        public bool HasDoT(SimulationState state, string name)
        {
            return state.DoTs.IsEffectActive(Effects[name]);
        }
        public float GetRemainingDoTTime(SimulationState state, string name)
        {
            return state.DoTs.GetRemainingTime(Effects[name]) ?? 0;
        }
        public void EndBuffEarly(SimulationState state, string name)
        {
            state.Buffs.EndEffect(Effects[name]);
        }
        public bool LastComboActionWas(SimulationState state, string name)
        {
            return state.LastComboAction == Actions[name];
        }
        public bool OnCooldown(SimulationState state, string name, float? duration = null)
        {
            return state.Cooldowns.IsCooldownRunning(name, duration);
        }
        public float? GetRemainingCooldownTime(SimulationState state, string name, float? cooldown = null)
        {
            return state.Cooldowns.GetRemainingTime(name, cooldown);
        }
        public bool HasPerformedAction(SimulationState state, string name)
        {
            return state.AllActions.ReverseHistory.Contains(Actions[name]);
        }
        #endregion
    }
}
