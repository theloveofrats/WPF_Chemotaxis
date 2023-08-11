using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Chemotaxis.Model;

namespace WPF_Chemotaxis.MitogensPlugin
{
    class MitosisParams
    {
        public double meanTime { get; set; }
        public double steepness { get; set; }

        private double nextTime;
        private double currentTime;
        
        private SciRand rnd;
        public SciRand Rnd
        {
            get
            {
                return rnd;
            }
        }

        public void Update(double dt)
        {
            currentTime += dt;
        }

        public bool ReadyToSplit()
        {
            //System.Diagnostics.Debug.Print(String.Format("nextTime {0:0.000}     curentTime {1:0.000}", nextTime, currentTime));

            return nextTime < currentTime;

        }
        public void ConfirmSplit()
        {
            currentTime = 0;
            SetNextSplit();
        }

        private void SetNextSplit()
        {
            double nextRnd = rnd.NextDouble();
            nextTime = meanTime - Math.Log(1d / nextRnd - 1d) / steepness;
        }
        public MitosisParams(int cellnum)
        {
            rnd = new SciRand(cellnum.GetHashCode() ^ System.DateTime.Now.Millisecond);
            SetNextSplit();
        }

        public void Update(CellComponent_Mitosis  component, Simulations.Simulation sim)
        {
            this.steepness = component.steepnessRange.RandomInRange;
            this.meanTime = component.meanTimeRange.RandomInRange;

            SetNextSplit();
            
        }

        public void ConnectToCellType(CellType ct)
        {

        }
    }
}
