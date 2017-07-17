using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    public abstract class WideSimulationParameters : SimulationDriver
    {
        public int CullInterval
        { get; private set; }

        protected WideSimulationParameters(JobMechanics job, int cullInterval) : base(job)
        { CullInterval = cullInterval; }

        public abstract (ResultState state, float score) GetResultScore(SimulationState result);

        public abstract IEnumerable<SimulationState> GetStepsToCull(IEnumerable<SimulationState> steps);

        public abstract IEnumerable<SimulationState> PrepareSimulations(SimulationState root);
    }
}
