using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    public class DeepSimulationDriverAttribute : Attribute
    {
        public string Name
        { get; private set; }

        public DeepSimulationDriverAttribute(string Name)
        {
            this.Name = Name;
        }
    }
}
