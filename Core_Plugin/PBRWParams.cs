using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Chemotaxis.CorePlugin
{
    public class PBRWParams
    {
        public double sigma { get; set; }
        public double chemokinesis { get; set; }
        public double chemotactic_strength { get; set; }

        private SciRand rnd;
        public SciRand Rnd
        {
            get
            {
                return rnd;
            }
        }

        public PBRWParams(int cellnum)
        {
            rnd = new SciRand(cellnum.GetHashCode() ^ System.DateTime.Now.Millisecond);
        }

        public void Update(CellLogic_PBRW logic, Simulations.Simulation sim)
        {
            double rp = logic.persistence.RandomInRange;
            this.sigma = -Math.Log(rp*rp) * Math.Sqrt(sim.Settings.dt);
            this.chemokinesis = logic.chemokinesis.RandomInRange;
            this.chemotactic_strength = logic.chemotaxis_power.RandomInRange;
        }
    }
}
