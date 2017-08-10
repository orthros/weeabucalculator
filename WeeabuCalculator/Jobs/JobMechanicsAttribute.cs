using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class JobMechanicsAttribute : ExportAttribute, IJobMechanicsMetadata
    {
        public string Name
        { get; private set; }

        public JobMechanicsAttribute(string Name) 
            : base(typeof(JobMechanics))
        {
            this.Name = Name;
        }
    }

}
