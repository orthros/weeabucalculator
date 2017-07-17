using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{

    public class StatusEffect
    {
        public string EffectID
        { get; private set; }

        public float? Duration
        { get; private set; }

        public StatusEffect(string effectID, float? duration)
        {
            this.EffectID = effectID;
            this.Duration = duration;
        }

        public override string ToString()
        {
            return EffectID;
        }
    }
}
