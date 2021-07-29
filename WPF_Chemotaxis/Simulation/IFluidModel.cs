using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Chemotaxis.Simulations
{
    public interface IFluidModel
    {
        public double[] GetVelocity(double x, double y);
        public double GetDivergence(double x, double y);
        public double[] GetVorticity(double x, double y);
    }
}
