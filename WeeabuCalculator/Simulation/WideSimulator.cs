using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    public class WideSimulator : TreeSimulation
    {
        public WideSimulationParameters Parameters
        { get; private set; }

        public WideSimulator(Player player, WideSimulationParameters simulationParameters, TextWriter outputStream) : base(new SimulationState(player, null), outputStream)
        {
            Parameters = simulationParameters;
        }

        public IEnumerable<SimulationState> RunSimulation()
        {
            var initialStates = Parameters.PrepareSimulations(RootState);

            RunSimulation(initialStates, 1);

            return GetLeaves(RootState);
        }

        private void RunSimulation(IEnumerable<SimulationState> steps, int stepNumber)
        {
            if (!steps.Any()) return;
            Output.WriteLine($"\nBeginning step {stepNumber}");

            foreach (var step in steps)
                step.PerformActions(Parameters.GetActionSuggestions(step));

            IEnumerable<SimulationState> nextSteps = (from s in steps
                                                      from s2 in s.NextSteps
                                                      let r = Parameters.GetResultScore(s2)
                                                      where r.state == ResultState.Inconclusive
                                                      orderby r.score descending
                                                      select s2);

            var conclusiveSteps = (from s in steps
                                   from s2 in s.NextSteps
                                   let r = Parameters.GetResultScore(s2)
                                   where r.state == ResultState.Conclusive
                                   orderby r.score descending
                                   select s2);

            Output.WriteLine($"Conclusive steps: {conclusiveSteps.Count()}");

            foreach (var r in conclusiveSteps.Take(5))
                WriteResult(r);

            var numNextSteps = nextSteps.Count();

            Output.WriteLine($"Next steps: {numNextSteps}");

            if (Parameters.CullInterval > 0 && stepNumber % Parameters.CullInterval == 0)
            {
                ClearTree(Parameters.GetStepsToCull(nextSteps).ToArray());

                var newNumNextSteps = nextSteps.Count();
                Output.WriteLine($"Culled steps = {numNextSteps - newNumNextSteps}");
            }

            foreach (var r in nextSteps.Take(5))
                WriteResult(r);

            RunSimulation(nextSteps, stepNumber + 1);
        }
    }
}
