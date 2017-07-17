using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    public static class MathHelper
    {
        public static float Trunc(float v, int decimalPlaces = 0)
        {
            float multiplier = (float)Math.Pow(10f, decimalPlaces);
            return (float)(Math.Truncate(v * multiplier) / multiplier);
        }

        public static float Round(float v, int decimalPlaces = 0)
        {
            return (float)Math.Round(v, decimalPlaces);
        }

    }
}
