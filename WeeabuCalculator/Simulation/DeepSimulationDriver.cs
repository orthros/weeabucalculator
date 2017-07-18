using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    public abstract class DeepSimulationDriver : SimulationDriver
    {
        public int TopSimulationsToKeep
        { get; protected set; }

        protected DeepSimulationDriver(JobMechanics job) : base(job)
        { }

        public abstract void HandleArguments(string[] args);

        public abstract (ResultState state, float score) GetResultScore(SimulationState result);

        public abstract (ResultState state, float score) GetInitialResultScore(SimulationState result);
    }
}
