using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    public abstract class StateBasedStorage<T>
    {

        public SimulationState CurrentState
        { get; private set; }

        private ValueAddedThisStep firstValue;
        private ValueAddedThisStep lastValue;

        public IEnumerable<T> ValuesAddedThisStep
        {
            get
            {
                var node = firstValue;
                while (node != null)
                {
                    yield return node.Value;
                    node = node.Next;
                }
            }
        }

        protected StateBasedStorage(SimulationState currentState)
        {
            this.CurrentState = currentState;
        }

        protected IEnumerable<T> GetHistory(SimulationState step = null)
        {
            if (step == null) step = CurrentState;

            // Unlike Alexander 11, we must go backwards first, then forward.
            if (step.PreviousStep != null) foreach (var d in GetHistory(step.PreviousStep)) yield return d;

            // Now we can go forward.
            foreach (var h in GetStorageFromStep(step).ValuesAddedThisStep) yield return h;
        }

        protected IEnumerable<T> GetReverseHistory(SimulationState step = null)
        {
            if (step == null) step = CurrentState;

            // First we go forward.
            foreach (var h in GetStorageFromStep(step).ValuesAddedThisStep.Reverse()) yield return h;

            // Then you go backward.
            if (step.PreviousStep != null) foreach (var d in GetReverseHistory(step.PreviousStep)) yield return d;
        }

        protected void AddValue(T value)
        {
            var newValueThisStep = new ValueAddedThisStep(value);
            if (firstValue == null) firstValue = newValueThisStep;
            else lastValue.Next = newValueThisStep;

            lastValue = newValueThisStep;
        }

        protected abstract StateBasedStorage<T> GetStorageFromStep(SimulationState step = null);

        private class ValueAddedThisStep
        {
            public T Value;
            public ValueAddedThisStep Next;

            public ValueAddedThisStep(T value)
            {
                Value = value;
            }
        }
    }
}
