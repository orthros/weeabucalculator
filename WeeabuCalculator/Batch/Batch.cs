using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace WeeabuCalculator
{
    public class Batch
    {
        public string Name { get; set; }
        public PlayerInfo Player { get; set; }
        public string Job { get; set; }
        public string Driver { get; set; }
        public string[] DriverArguments { get; set; }

        public static DeepSimulationDriver BuildDriver(Batch batch, JobMechanics job)
        {
            try
            {
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (var t in a.GetTypes())
                        {
                            try
                            {
                                if (typeof(DeepSimulationDriver).IsAssignableFrom(t))
                                {
                                    var attr = t.GetCustomAttributes(typeof(DeepSimulationDriverAttribute), true);
                                    if (attr.Any())
                                    {
                                        if (((DeepSimulationDriverAttribute)attr.First()).Name == batch.Driver)
                                        {
                                            var constructor = t.GetConstructor(new Type[] { typeof(JobMechanics) });
                                            if (constructor != null)
                                            {
                                                var driver = constructor.Invoke(new object[] { job });
                                                return (DeepSimulationDriver) driver;
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex3) { }
                        }
                    }
                    catch (Exception ex2) { }
                }
            }
            catch (Exception ex) { }

            return null;
        }

        public static JobMechanics BuildJob(Batch batch)
        {
            try
            {
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (var t in a.GetTypes())
                        {
                            try
                            {
                                if (typeof(JobMechanics).IsAssignableFrom(t))
                                {
                                    var attr = t.GetCustomAttributes(typeof(JobMechanicsAttribute), true);
                                    if (attr.Any())
                                    {
                                        if (((JobMechanicsAttribute)attr.First()).Name == batch.Job)
                                        {
                                            var constructor = t.GetConstructor(new Type[] { });
                                            if (constructor != null)
                                            {
                                                var job = constructor.Invoke(new object[] { });
                                                return (JobMechanics) job;
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex3) { }
                        }
                    }
                    catch (Exception ex2) { }
                }
            }
            catch (Exception ex) { }

            return null;
        }

        public static IEnumerable<Batch> ReadFile(string path)
        {
            using (var r = new StreamReader(path))
            {
                var d = new Deserializer();
                return d.Deserialize<Batches>(r).Root;
            }
        }

        private class Batches
        {
            public Batch[] Root { get; set; }
        }
    }
}
