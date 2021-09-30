using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Chemotaxis.Simulations
{
    public interface IFluidModel
    {
        public void Initialise(Simulation sim, Environment env);

        public void SetPressure(int x, int y, double p);

        public void Update(Simulation sim, Environment env, double dt);
        public double[] GetVelocity(double x, double y);
        public double GetDivergence(double x, double y);
        public double[] GetVorticity(double x, double y);
    }
}
