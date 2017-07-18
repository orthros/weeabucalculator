using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    class Program
    {
        internal static int INITIAL_PRUNE_DEPTH = 15;
        internal static int PRUNE_DEPTH = 5; // steps deep to prune.
        internal static float PRUNE_LOWER_PERCENT = 0.99f; // lower % to prune;


        static void Main(string[] args)
        {
            Console.WriteLine("Reading batch.txt file...");
            IEnumerable<Batch> batches = null;
            try
            {
                batches = Batch.ReadFile("batch.txt");
            }
            catch (Exception ex) { }

            if (batches == null)
            {
                Console.WriteLine("batches.txt not read!");
                return;
            }

            Console.WriteLine($"Found {batches.Count()} batches.");

            foreach (var b in batches)
                RunBatch(b);

        }

        private static void RunBatch(Batch b)
        {
            Console.WriteLine($"Running batch {b}");
            Console.WriteLine("Building batch objects...");
            var job = Batch.BuildJob(b);
            var player = PlayerInfo.BuildPlayer(b.Player, job);
            var driver = Batch.BuildDriver(b, job);
            
            var startTime = DateTime.Now;
            var root = new SimulationState(player, null);
            var sim = new DeepSimulator(player, new SamuraiSimulationDriver(job), Console.Out, root);
            
            sim.FoundTopPerformer += (s, e) =>
            {
                Console.WriteLine($"{(DateTime.Now - startTime):hh\\:mm\\:ss} :: New top openner found! score: {e.score}");
            };

            var timerStep = 30f;
            var historyLength = 10;
            var t = new System.Timers.Timer(timerStep * 1000);
            var progressHistory = new Queue<(long progress, DateTime time)>(historyLength);
            t.Elapsed += (o, e) =>
            {
                AnnounceProgress(sim, ref progressHistory, historyLength, startTime);
            };
            t.Start();

            Console.WriteLine("Beginning simulation...");
            sim.RunSimulation();
            t.Stop();

            var dir = $"batchresults/{b.Name}/";
            var filename = $"{dir}{DateTime.Now:yyyy-mm-dd hh-mm-ss}.result";
            Console.WriteLine($"Saving output to {filename}");
            Directory.CreateDirectory(dir);
            using (var w = new StreamWriter(filename))
            {
                foreach (var result in (from l in TreeSimulation.GetLeaves(root) let r = driver.GetResultScore(l) orderby r.score descending select (state: l, score: r.score)))
                {
                    w.WriteLine($"{result.score} || {result.state.AllActions.HistoryString}");
                }
            }
        }

        public static void AnnounceProgress(DeepSimulator sim, ref Queue<(long progress, DateTime time)> progressHistory, int historyLength, DateTime startTime)
        {
            var pctComplete = sim.CompletedStartingPaths / (double)sim.TotalStartingPoints * 100;
            if (double.IsNaN(pctComplete) && pctComplete == 0) return;

            progressHistory.Enqueue((sim.CompletedStartingPaths, DateTime.Now));
            if (progressHistory.Count > historyLength) progressHistory.Dequeue();
            var progressOverHistory = progressHistory.First().progress - progressHistory.Last().progress;
            var durationOfHistory = progressHistory.First().time - progressHistory.Last().time;
            var completeTime = (sim.TotalStartingPoints - sim.CompletedStartingPaths) * (durationOfHistory.TotalSeconds / progressOverHistory);
            if (double.IsNaN(completeTime)) completeTime = 0;
            Console.WriteLine($"{(DateTime.Now - startTime):hh\\:mm\\:ss} :: {sim.CompletedStartingPaths}/{sim.TotalStartingPoints}, {pctComplete:0.00}%, complete in {TimeSpan.FromSeconds(completeTime):hh\\:mm\\:ss}");
        }

        public static void test()
        { 
            var job = new SamuraiJobMechanics();
            var player = new Player(job)
            {
                Speed = 900
            };

            DateTime _startTime;
            var root = new SimulationState(player, null);
            System.Timers.Timer t = new System.Timers.Timer(10000);

            Console.Write("Beginning simulation... load previous results? (y/n) ");
            if (Console.ReadKey().Key == ConsoleKey.Y)
            {
                Console.WriteLine("\nLoading previous simulation...");
                using (var r = new StreamReader("out.txt"))
                {
                    while (!r.EndOfStream)
                    {
                        var line = r.ReadLine();
                        var result = job.ReadResult(line);
                        AddResultToNode(root, result);
                    }
                }
            }
            else
            {
                Console.WriteLine("Running new simulation...");

                _startTime = DateTime.Now;
                var openerSim = new DeepSimulator(player, new SamuraiOpenerSimulationDriver(job), Console.Out, root);

                t.Elapsed += (o, e) =>
                {
                    var pctComplete = openerSim.CompletedStartingPaths / (double)openerSim.TotalStartingPoints * 100;
                    Console.WriteLine($"Status: completed starting points = {openerSim.CompletedStartingPaths}, {pctComplete:0.00}% complete");
                };
                t.Start();

                openerSim.RunSimulation();
                t.Stop();

                Console.WriteLine("Simulation complete... press any key to exit.");
                Console.ReadKey();

                var results = openerSim.Results.ToArray();
                Console.WriteLine($"Writing {results.Length} results to out.txt");

                using (var w = new StreamWriter("out.txt"))
                {
                    foreach (var r in results)
                    {
                        var s = openerSim.Driver.GetResultScore(r);
                        if (s.state == ResultState.Conclusive) w.WriteLine(r.AllActions.HistoryString);
                    }
                }
            }

            Console.WriteLine("Running rotation simulation...");

            _startTime = DateTime.Now;
            var sim = new DeepSimulator(player, new SamuraiSimulationDriver(job), Console.Out, root);
            

            sim.FoundTopPerformer += (s, e) =>
            {
                Console.WriteLine($"{(DateTime.Now - _startTime):hh\\:mm\\:ss} :: New top openner found! score: {e.score}");
            };
            var timerStep = 10f;
            var historyLength = 10;
            t = new System.Timers.Timer(timerStep * 1000);
            var progressHistory = new Queue<(long progress, DateTime time)>(historyLength);
            t.Elapsed += (o, e) =>
            {
                var pctComplete = sim.CompletedStartingPaths / (double)sim.TotalStartingPoints * 100;

                progressHistory.Enqueue((sim.CompletedStartingPaths, DateTime.Now));
                if (progressHistory.Count > historyLength) progressHistory.Dequeue();
                var progressOverHistory = progressHistory.First().progress - progressHistory.Last().progress;
                var durationOfHistory = progressHistory.First().time - progressHistory.Last().time;
                var completeTime = (sim.TotalStartingPoints - sim.CompletedStartingPaths) * (durationOfHistory.TotalSeconds / progressOverHistory);
                if (double.IsNaN(completeTime)) completeTime = 0;
                Console.WriteLine($"{(DateTime.Now - _startTime):hh\\:mm\\:ss} :: {sim.CompletedStartingPaths}/{sim.TotalStartingPoints}, {pctComplete:0.00}%, complete in {TimeSpan.FromSeconds(completeTime):hh\\:mm}");
            };
            t.Start();

            sim.RunSimulation();

            Console.WriteLine("Simulation complete... press any key to exit.");
            Console.ReadKey();

        }

        private static void AddResultToNode(SimulationState root, IEnumerable<PlayerAction> result)
        {
            if (!result.Any()) return;

            foreach (var nextAction in root.NextSteps.ToArray())
            {
                if (nextAction.AllActions.ValuesAddedThisStep.FirstOrDefault().Action == result.First())
                {
                    AddResultToNode(nextAction, result.Skip(1));
                    return;
                }
            }

            var newStep = root.Clone(root);
            newStep.PerformAction(result.First());
            root.NextSteps.Add(newStep);
            AddResultToNode(newStep, result.Skip(1));
        }

        private static void WriteResult(SimulationState result, StreamWriter sw)
        {
            string rString = result.AllActions.HistoryString;
            Console.WriteLine(rString);
            if (sw != null) sw.Write(sw);
        }
    }
}
