using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportPipesTestsCommon.Helpers
{
    public static class TestHelper
    {
        //Ticks goes fine to up about 1 year (3600 * 24 * 365), when I do this * 10 we get negative int exceptions.
        public static int Ticks = 3600;

        public static bool AlmostEqual(double value1, double value2)
        {
            return Math.Abs(value1 - value2) <= 0.0000001;
        }
    }
}
