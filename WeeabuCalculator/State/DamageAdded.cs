using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    public class DamageAdded
    {
        public float Damage
        { get; private set; }

        public float Time
        { get; private set; }

        public DamageAdded(float damage, float time)
        {
            this.Damage = damage;
            this.Time = time;
        }
    }
}
