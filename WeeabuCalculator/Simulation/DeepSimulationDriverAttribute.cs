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
    public class SimulationDriverAttribute : ExportAttribute, ISimulationDriverMetadata
    {
        public string Name
        { get; private set; }

        public SimulationDriverAttribute(string Name)
            : base(typeof(DeepSimulationDriver))
        {
            this.Name = Name;
        }
    }
}
