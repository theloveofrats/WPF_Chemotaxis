using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Chemotaxis.Simulations
{
   /* /// <summary>
    /// Class supplying an empty fluid model (returns zero movement for everything).
    /// </summary>
    class FluidModel_LBMD2Q5 : IFluidModel
    {

        private Environment env;
        private Simulation sim;

        double[] v0, v1, v2, v3, v4, v5, v6, v7, v8, v9;

        void IFluidModel.Initialise(Simulation sim, Environment env)
        {
            this.sim = sim;
            this.env = env;

            this.ux = new double[(env.Width + 1) * env.Height];
            this.uy = new double[env.Width * (env.Height + 1)];
            this.p = new double[env.Width * env.Height];
        }

        void IFluidModel.Update(Simulation sim, Environment env, double dt)
        {

        }

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

        void IFluidModel.SetPressure(int x, int y, double p)
        {
            this.p[y * env.Width + x] = p;
        }

        private void CalculateSteadyStateFlow()
        {
            double tolerance = 0;
            double dx = env.settings.DX;

            /* x continuum:
             * dpdx = (pw-pe)dy (dy is the same as dx)
             * 

            
        }
    }*/
}
