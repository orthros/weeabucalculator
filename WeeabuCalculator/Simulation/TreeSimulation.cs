using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    public abstract class TreeSimulation
    {

        public SimulationState RootState
        { get; private set; }

        public TextWriter Output
        { get; private set; }

        public IEnumerable<SimulationState> Results
        { get { return GetLeaves(RootState); } }

        protected TreeSimulation(SimulationState root, TextWriter output)
        {
            RootState = root;
            Output = output;
        }

        protected void ClearTree(SimulationState step)
        {
            if (step == null) return; // we're done

            step.NextSteps.Clear();

            if (step.PreviousStep != null)
            {
                // remove me from the previous step's next steps.
                step.PreviousStep.NextSteps.Remove(step);

                // If my previous step doesn't have any next steps, remove them.
                if (!step.PreviousStep.NextSteps.Any()) ClearTree(step.PreviousStep);

                step.PreviousStep = null;
            }
        }

        protected void ClearTree(IEnumerable<SimulationState> steps)
        {
            foreach (var step in steps)
                ClearTree(step);
        }

        public static IEnumerable<SimulationState> GetLeaves(SimulationState initialNode)
        {
            if (!initialNode.NextSteps.Any())
            {
                yield return initialNode;
                yield break;
            }

            foreach (var node in initialNode.NextSteps.ToArray())
            {
                foreach (var leaf in GetLeaves(node)) yield return leaf;
            }
        }

        protected void WriteResult(SimulationState result)
        {
            string rString = "";
            foreach (var a in result.AllActions.History)
            {
                if (a.GCD) rString += a.Name;
                else rString += $"({a.Name})";

                rString += " > ";
            }
            Output.WriteLine(rString);
        }
    }
}
