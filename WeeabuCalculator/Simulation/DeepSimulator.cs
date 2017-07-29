using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    public class DeepSimulator : TreeSimulation
    {
        private DateTime _startTime;
        public long CompletedPaths { get; private set; }
        public long DeadPaths { get; private set; }
        public long NodesInAction { get; private set; }
        public long NumSplits { get; private set; }
        public double AverageSplits { get; private set; }
        public long TotalCompleteLength { get; private set; }
        public long TotalStartingPoints { get; private set; }
        public long CompletedStartingPaths { get; private set; }
        public double AverageStartingPointLength { get; private set; }
        public double AverageCompleteLength { get; private set; }

        public TopScoreTracker TopScores
        { get; private set; }

        public long EstimatedTotalEndpoints
        {
            get
            {
                return (long)Math.Pow(AverageSplits, AverageCompleteLength - AverageStartingPointLength) * TotalStartingPoints;
            }
        }
        public long EstimatedCompletedEndpoints
        {
            get
            {
                return CompletedPaths + DeadPaths;
            }
        }

        public event EventHandler<(float score, SimulationState state)> FoundTopPerformer;
        public event EventHandler StatisticsChanged;

        public DeepSimulationDriver Driver
        { get; private set; }

        public DeepSimulator(Player player, DeepSimulationDriver driver, TextWriter outputStream) : this(player, driver, outputStream, new SimulationState(player, null))
        { }

        public DeepSimulator(Player player, DeepSimulationDriver driver, TextWriter outputStream, SimulationState root) : base(root, outputStream)
        {
            Driver = driver;
            if (driver.TopSimulationsToKeep > 0) TopScores = new TopScoreTracker(driver.TopSimulationsToKeep);
        }

        public void RunSimulation()
        {
            _startTime = DateTime.Now;
            Output.WriteLine("Generating starting points...");
            var startingPoints = Driver.GenerateInitialStates(RootState).ToArray();
            TotalStartingPoints = startingPoints.Length;
            AverageStartingPointLength = (from sp in startingPoints select sp.Item3.StepNumber).Sum() / (double)TotalStartingPoints;
            OnStatisticsChanged();
            Output.WriteLine($"Discovered {TotalStartingPoints} starting points. Beginning Depth-first simulation. ");

            var tasks = new List<Task>((int)TotalStartingPoints);
            foreach (var s in (from n in startingPoints orderby n.Item2 descending select n.Item3))
            {
                var t = new Task(() =>
                {
                    RunSimulation(s);
                    CompletedStartingPaths++;
                    OnStatisticsChanged();
                });
                t.Start();
                tasks.Add(t);
            }

            Task.WaitAll(tasks.ToArray());
        }

        protected virtual void OnStatisticsChanged()
        {
            if (StatisticsChanged != null) StatisticsChanged.Invoke(this, null);
        }

        private void RunSimulation(SimulationState step)
        {
            step.PerformActions(Driver.GetActionSuggestions(step));

            var nextSteps = (from s in step.NextSteps.ToArray() let r = Driver.GetResultScore(s) orderby r.score descending select (r, s)).ToArray();

            var tasks = new List<Task>(nextSteps.Length);
            foreach (var s in nextSteps)
            {
                NodesInAction++;
                NumSplits += nextSteps.Length;
                AverageSplits = NumSplits / (double)NodesInAction;
                OnStatisticsChanged();

                var nextStep = s.Item2;
                var result = s.Item1;

                switch (result.state)
                {
                    case ResultState.Conclusive:
                        CompletedPaths++;
                        TotalCompleteLength += nextStep.StepNumber;
                        AverageCompleteLength = TotalCompleteLength / (double)CompletedPaths;
                        OnStatisticsChanged();

                        RecordTopValue(nextStep, result.score);

                        break;
                    case ResultState.Inconclusive:
                        // Run simulation from this step.
                        var t = new Task(() => RunSimulation(nextStep));
                        t.Start();
                        tasks.Add(t);
                        break;
                    case ResultState.Dead:
                        DeadPaths++;
                        OnStatisticsChanged();
                        break;
                }
            }

            Task.WaitAll(tasks.ToArray());

            foreach (var l in GetLeaves(step))
            {
                var r = Driver.GetResultScore(l);
                if (r.state != ResultState.Conclusive)
                    ClearNode(l, step);
            }
        }

        private void ClearNode(SimulationState l, SimulationState step)
        {
            if (l == step) return;

            if (l.PreviousStep != null)
                l.PreviousStep.NextSteps.Remove(l);

            ClearNode(l.PreviousStep, step);
        }

        private void RecordTopValue(SimulationState result, float score)
        {
            if (TopScores == null) return;

            var foundTopPerformer = !TopScores.TopScore.HasValue || score > TopScores.TopScore.Value.score;
            if (foundTopPerformer && FoundTopPerformer != null) FoundTopPerformer.Invoke(this, (score, result));

            var oustedValue = TopScores.Insert(result, score);
            if (oustedValue.HasValue)
            {
                var state = oustedValue.Value.state;

                if (state.PreviousStep != null)
                {
                    lock (state.PreviousStep)
                    {
                        if (state.PreviousStep != null && state.PreviousStep.NextSteps.Contains(state)) state.PreviousStep.NextSteps.Remove(state);
                        state.PreviousStep = null;
                    }
                }
            }
        }

        private IEnumerable<(ResultState, float, SimulationState)> GenerateStartingPoints(IEnumerable<SimulationState> steps)
        {
            foreach (var step in steps)
                step.PerformActions(Driver.GetActionSuggestions(step));

            var nextSteps = (from s in steps
                             from s2 in s.NextSteps
                             let r = Driver.GetInitialResultScore(s)
                             select (r.state, r.score, s2));

            foreach (var nextStep in nextSteps)
            {
                if (nextStep.Item1 == ResultState.Conclusive)
                    yield return nextStep;
            }

            var inconclusiveSteps = (from s in nextSteps where s.Item1 == ResultState.Inconclusive select s.Item3);

            if (inconclusiveSteps.Any())
                foreach (var n in GenerateStartingPoints(inconclusiveSteps)) yield return n;
        }
    }
}
