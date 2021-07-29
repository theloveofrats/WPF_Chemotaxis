using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Chemotaxis.UX
{
    public interface IHeatMapSource
    {
        public byte GetIntensity(Simulations.Environment environment, int x, int y);

        public double Min { get; set; }
        public double Max { get; set; }
        public List<HeatMapMethodAttribute> GetIntensityMethods();

        public void SetMethod(int i, out double range_start, out double range_end);
    }
}
