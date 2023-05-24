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
    /*class FluidModel_SteadyStateSIMPLE : IFluidModel
    {

        private Environment env;
        private Simulation sim;

        //protected double[] ux;
        //protected double[] uy;
        //protected double[] p;

        void IFluidModel.Initialise(Simulation sim, Environment env)
        {
            this.sim = sim;
            this.env = env;

            
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

            

            double[] ux, uy, p;
            ux = new double[(env.Width + 1) * env.Height];
            uy = new double[env.Width * (env.Height+1)];
            p = new double[env.Width * env.Height];
        }
    }*/
}
