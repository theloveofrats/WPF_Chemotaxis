using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Chemotaxis
{
    public static class MathExtensions
    {
        public static bool Approx(this double a, double b, double tolerance = 0.001)
        {
            return Math.Abs(a - b) <= tolerance;
        }
    }
}
