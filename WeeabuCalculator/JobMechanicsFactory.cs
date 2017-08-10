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
    public class JobMechanicFactory
    {
        [ImportMany]
        private IEnumerable<Lazy<JobMechanics,IJobMechanicsMetadata>> _jobMechanics = null;

        private IEnumerable<Lazy<JobMechanics,IJobMechanicsMetadata>> JobMechanics
        {
            get
            {
                return _jobMechanics;
            }
        }

        public JobMechanicFactory()
        {
            AssemblyCatalog ac = new AssemblyCatalog(assembly: Assembly.GetExecutingAssembly());            

            var container= new CompositionContainer(ac);
            container.ComposeParts(this);            
        }

        public JobMechanics BuildJobFromBatch(Batch batch)
        {
            var targetJob = JobMechanics.FirstOrDefault(x => x.Metadata.Name.Equals(batch.Job));
            return targetJob?.Value;
        }
    }
}
