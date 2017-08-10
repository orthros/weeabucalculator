using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    public class DeepSimulationDriverFactory
    {
        [ImportMany]
        private IEnumerable<Lazy<DeepSimulationDriver, ISimulationDriverMetadata>> _drivers = null;

        private IEnumerable<Lazy<DeepSimulationDriver, ISimulationDriverMetadata>> Drivers
        {
            get
            {
                return _drivers;
            }
        }

        public DeepSimulationDriverFactory(JobMechanics job)
        {
            AssemblyCatalog ac = new AssemblyCatalog(assembly: Assembly.GetExecutingAssembly());

            var container = new CompositionContainer(ac);
            container.ComposeExportedValue(job);
            container.ComposeParts(this);
        }

        public DeepSimulationDriver BuildJobFromBatch(Batch batch)
        {
            var targetDriver = Drivers.FirstOrDefault(x => x.Metadata.Name.Equals(batch.Driver));
            targetDriver?.Value?.HandleArguments(batch.DriverArguments);
            return targetDriver?.Value;
        }
    }
}
