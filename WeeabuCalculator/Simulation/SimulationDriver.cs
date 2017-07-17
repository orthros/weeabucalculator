using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    public abstract class SimulationDriver
    {
        protected JobMechanics Job
        { get; private set; }

        protected SimulationDriver(JobMechanics job)
        { Job = job; }

        /// <summary>
        /// Generates an array of potential actions from the current player state. To be effective,
        /// this should be as big of a list as possible.
        /// </summary>
        /// <param name="player">The player object that is performing an action</param>
        /// <returns></returns>
        public abstract IEnumerable<PlayerAction> GetActionSuggestions(SimulationState state);

        public abstract IEnumerable<(ResultState state, float score, SimulationState step)> GenerateInitialStates(SimulationState root);

    }


    public enum ResultState
    {
        Inconclusive, // run more
        Conclusive, // completed
        Dead, // completed, but bad.
    }
}
