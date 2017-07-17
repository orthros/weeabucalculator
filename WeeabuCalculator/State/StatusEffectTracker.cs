using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    public class StatusEffectTracker : StateBasedStorage<StatusEffectEvent>
    {
        private Func<SimulationState, StateBasedStorage<StatusEffectEvent>> _getStorageMechanismDelegate;

        public IEnumerable<StatusEffect> ActiveEffects
        { get { return GetActiveEffects(); } }

        public StatusEffectTracker(SimulationState currentState, Func<SimulationState, StateBasedStorage<StatusEffectEvent>> getStorageMechanismDelegate) : base(currentState)
        {
            _getStorageMechanismDelegate = getStorageMechanismDelegate;
        }

        /// <summary>
        /// Checks if this effect is active.
        /// </summary>
        /// <param name="effect"></param>
        /// <returns></returns>
        public bool IsEffectActive(StatusEffect effect)
        {
            return IsEffectActive(effect.EffectID, effect.Duration);
        }

        /// <summary>
        /// Checks if effect is active by name.
        /// </summary>
        /// <param name="effectID">The effect name to check for</param>
        /// <param name="effectDuration">The optional duration to seach for. If none specified, will look until the beginning of time.</param>
        /// <returns></returns>
        public bool IsEffectActive(string effectID, float? effectDuration = null)
        {
            var remainingTime = GetRemainingTime(effectID, effectDuration);
            return !remainingTime.HasValue || remainingTime.Value > 0;
        }

        /// <summary>
        /// Activates this effect
        /// </summary>
        /// <param name="effect"></param>
        public void ActivateEffect(StatusEffect effect)
        {
            AddValue(new StatusEffectEvent(CurrentState.CurrentTime, effect, StatusEffectEventType.Added));

            CurrentState.Log.Write(CurrentState.CurrentTime, $"{effect.EffectID} is applied.");
            CurrentState.Player.Job.OnEffectEvent(effect, StatusEffectEventType.Added, CurrentState);
        }

        /// <summary>
        /// Ends this effect
        /// </summary>
        /// <param name="effect"></param>
        public void EndEffect(StatusEffect effect)
        {
            if (!IsEffectActive(effect)) return;

            AddValue(new StatusEffectEvent(CurrentState.CurrentTime, effect, StatusEffectEventType.Ended));

            CurrentState.Log.Write(CurrentState.CurrentTime, $"{effect.EffectID} wears off.");
            CurrentState.Player.Job.OnEffectEvent(effect, StatusEffectEventType.Ended, CurrentState);
        }

        /// <summary>
        /// Gets the remaining time of this effect. Returns null if effect is active, but has no duration.
        /// </summary>
        /// <param name="effectID"></param>
        /// <param name="effectDuration"></param>
        /// <returns>Returns 0 if effect is inactive, null if effect is active, but has no duration.</returns>
        public float? GetRemainingTime(StatusEffect effect)
        {
            return GetRemainingTime(effect.EffectID, effect.Duration);
        }

        /// <summary>
        /// Gets the remaining time of this effect. Returns null if effect is active, but has no duration.
        /// </summary>
        /// <param name="effectID"></param>
        /// <param name="effectDuration"></param>
        /// <returns>Returns 0 if effect is inactive, null if effect is active, but has no duration.</returns>
        public float? GetRemainingTime(string effectID, float? effectDuration = null)
        {
            // step backwards through history
            foreach (var statusEvent in GetReverseHistory())
            {
                // Timeout the search if the effect duration is longer than this effect's time.
                if (effectDuration.HasValue)
                {
                    if (effectDuration.Value < CurrentState.CurrentTime - statusEvent.Time) return 0;
                }

                // This is not the correct effect.
                if (statusEvent.Effect.EffectID != effectID) continue;

                // The effect's last event type was an "end"
                if (statusEvent.EventType == StatusEffectEventType.Ended)
                    return 0;

                // The effect's last event type was an "add"
                if (statusEvent.EventType == StatusEffectEventType.Added)
                {
                    if (!statusEvent.Effect.Duration.HasValue) return null; // The effect is active, but it has no remaining time.

                    // Returns gets the remaining time if > 0 or 0
                    return Math.Max(statusEvent.Effect.Duration.Value - (CurrentState.CurrentTime - statusEvent.Time), 0);
                }
            }

            // Could not find effect, it must not be active.
            return 0f;
        }

        /// <summary>
        /// Gets all currently active effects for this step.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="maxHistory"></param>
        /// <returns></returns>
        private IEnumerable<StatusEffect> GetActiveEffects(SimulationState step, float? maxHistory = null)
        {
            // step backwards through history
            foreach (var statusEvent in GetReverseHistory())
            {
                // current event is older than max history, we're done looking
                if (maxHistory.HasValue && CurrentState.CurrentTime - statusEvent.Time > maxHistory) break;

                // if the event was added at this step, and if the effect is still active NOW, return it.
                if (statusEvent.EventType == StatusEffectEventType.Added)
                {
                    if (IsEffectActive(statusEvent.Effect)) yield return statusEvent.Effect;
                }
            }
        }

        /// <summary>
        /// Gets all currently active effects.
        /// </summary>
        /// <param name="maxHistory">Maximum amount of time to look backwards</param>
        /// <returns></returns>
        public IEnumerable<StatusEffect> GetActiveEffects(float? maxHistory = null)
        {
            return GetActiveEffects(CurrentState, maxHistory).Distinct();
        }

        protected override StateBasedStorage<StatusEffectEvent> GetStorageFromStep(SimulationState step = null)
        {
            return _getStorageMechanismDelegate.Invoke(step ?? CurrentState);
        }
    }

    public struct StatusEffectEvent
    {
        public StatusEffect Effect
        { get; private set; }

        public StatusEffectEventType EventType
        { get; private set; }

        public float Time
        { get; private set; }

        public StatusEffectEvent(float time, StatusEffect effect, StatusEffectEventType type)
        {
            this.Time = time;
            this.Effect = effect;
            this.EventType = type;
        }
    }

    public enum StatusEffectEventType
    {
        Added,
        Ended
    }

}
