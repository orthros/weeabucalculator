using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    public class DamageTracker : StateBasedStorage<DamageAdded>
    {
        /// <summary>
        /// Total damage/current time
        /// </summary>
        public float DPS
        {
            get
            {
                if (CurrentState.CurrentTime == 0) return 0;
                return (float)(TotalDamage / CurrentState.CurrentTime);
            }
        }

        private double? _totalDamageCache;
        /// <summary>
        /// Total damage since the beginning of time.
        /// </summary>
        public double TotalDamage
        {
            get
            {
                if (!_totalDamageCache.HasValue) _totalDamageCache = GetTotalDamage();
                return _totalDamageCache.Value;
            }
        }

        /// <summary>
        /// Gets the entire history of damage since the beginning.
        /// </summary>
        public IEnumerable<DamageAdded> DamageHistory
        { get { return GetHistory(); } }

        public DamageTracker(SimulationState currentState) : base(currentState)
        { }

        /// <summary>
        /// Get total damage from this step to the beginning of time.
        /// </summary>
        /// <param name="step"></param>
        /// <returns></returns>
        private double GetTotalDamage(SimulationState step = null)
        {
            if (step == null) step = CurrentState;

            // Sum our current step's damage
            var thisStepsDamage = (from d in ValuesAddedThisStep select (double)d.Damage).Sum();

            // We don't have a previous step, so just return this step's damage.
            if (step.PreviousStep == null) return thisStepsDamage;

            // Add our current damage to the last step's damage
            return step.PreviousStep.Damage.TotalDamage + thisStepsDamage;
        }

        public void AddDamage(float damage, float time)
        {
            AddValue(new DamageAdded(damage, time));
        }

        protected override StateBasedStorage<DamageAdded> GetStorageFromStep(SimulationState step = null)
        {
            return (step ?? CurrentState).Damage;
        }
    }
}
