using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Chemotaxis.Simulations
{
    /// <summary>
    /// Class supplying an empty fluid model (returns zero movement for everything).
    /// </summary>
    class FluidModel_None : IFluidModel
    {
        double IFluidModel.GetDivergence(double x, double y)
        {
            return 0;
        }

        double[] IFluidModel.GetVelocity(double x, double y)
        {
            return new double[] { 0, 0 };
        }

        double[] IFluidModel.GetVorticity(double x, double y)
        {
            return new double[] { 0, 0, 0 };
        }
    }
}
