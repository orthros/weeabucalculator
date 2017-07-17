using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    public class CooldownTracker : StateBasedStorage<CooldownEvent>
    {
        public CooldownTracker(SimulationState currentState) : base(currentState)
        { }

        protected override StateBasedStorage<CooldownEvent> GetStorageFromStep(SimulationState step = null)
        {
            return step.Cooldowns;
        }

        public void StartCooldown(string id, float duration)
        {
            AddValue(new CooldownEvent(id, CurrentState.CurrentTime, duration));
        }

        public bool IsCooldownRunning(string id, float? duration = null)
        {
            var remainingTime = GetRemainingTime(id, duration);
            return remainingTime > 0;
        }

        /// <summary>
        /// Gets the remaining time of this effect. Returns null if effect is active, but has no duration.
        /// </summary>
        /// <param name="effectID"></param>
        /// <param name="cooldown"></param>
        /// <returns>Returns 0 if effect is inactive, null if effect is active, but has no duration.</returns>
        public float GetRemainingTime(string id, float? cooldown = null)
        {
            var cdEvent = GetEvent(id, cooldown);

            if (cdEvent == null) return 0f;
            else return Math.Max(cdEvent.Duration - (CurrentState.CurrentTime - cdEvent.Time), 0);
        }

        /// <summary>
        /// Get the time this cooldown began.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cooldown"></param>
        /// <returns></returns>
        public float? GetApplicationTime(string id, float? cooldown= null)
        {
            var cdEvent = GetEvent(id, cooldown);

            return cdEvent?.Time;
        }

        private CooldownEvent GetEvent(string id, float? cooldown = null)
        {
            // step backwards through history
            foreach (var cooldownEvent in GetReverseHistory())
            {
                // Timeout the search if the effect duration is longer than this effect's time.
                if (cooldown.HasValue)
                {
                    if (cooldown.Value < CurrentState.CurrentTime - cooldownEvent.Time) return null;
                }

                // This is not the correct cooldown.
                if (cooldownEvent.CooldownID != id) continue;

                // Returns true if the duration of the cooldown isn't greater than the difference between time now and the time it was added.
                return cooldownEvent;
            }

            // Could not find effect, it must not be active.
            return null;

        }
    }

    public class CooldownEvent
    {
        public string CooldownID
        { get; private set; }

        public float Time
        { get; private set; }

        public float Duration
        { get; private set; }

        public CooldownEvent(string id, float time, float duration)
        {
            this.CooldownID = id;
            this.Time = time;
            this.Duration = duration;
        }
    }
}
