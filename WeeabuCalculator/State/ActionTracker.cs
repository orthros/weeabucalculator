using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    public class ActionTracker : StateBasedStorage<ActionPerformed>
    {
        private Func<SimulationState, ActionTracker> _getStorageMechanismDelegate;

        public IEnumerable<PlayerAction> History
        { get { return (from ap in GetHistory() select ap.Action); } }

        public string HistoryString
        { get { return string.Join(" > ", (from ap in History let apString = (ap.GCD ? ap.ToString() : $"[{ap.ToString()}]") select apString)); } }

        public IEnumerable<PlayerAction> ReverseHistory
        { get { return (from ap in GetReverseHistory() select ap.Action); } }

        public ActionTracker(SimulationState currentState, Func<SimulationState, ActionTracker> getStorageMechanismDelegate) : base(currentState)
        {
            _getStorageMechanismDelegate = getStorageMechanismDelegate;
        }

        public virtual void RecordActionPerformed(PlayerAction action, float damage, PlayerDoT dot = null)
        {
            AddValue(new ActionPerformed(action, damage, dot, CurrentState.CurrentTime));
        }

        public PlayerAction GetLastAction()
        {
            return ReverseHistory.FirstOrDefault();
        }
        
        protected override StateBasedStorage<ActionPerformed> GetStorageFromStep(SimulationState step = null)
        {
            return _getStorageMechanismDelegate.Invoke(step ?? CurrentState);
        }

        public IEnumerable<ActionPerformed> GetActionsBetween(float startTime, float endTime)
        {
            foreach(var ap in GetReverseHistory())
            {
                if (ap.Time >= startTime && ap.Time <= endTime) yield return ap;
            }
        }
    }

    public struct ActionPerformed
    {
        public PlayerAction Action
        { get; private set; }

        public float Time
        { get; private set; }

        public float Damage
        { get; private set; }

        public PlayerDoT DoT
        { get; private set; }

        public ActionPerformed(PlayerAction action, float damage, PlayerDoT dot, float time)
        {
            Action = action;
            Damage = damage;
            DoT = dot;
            Time = time;
        }

    }
}
