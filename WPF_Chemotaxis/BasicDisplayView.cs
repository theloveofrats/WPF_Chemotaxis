using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Chemotaxis.UX
{
    internal class BasicDisplayView : IHeatMapSource
    {

        public string Name { get; private set; } = "Structure view";
        public double Min { get; set; }
        public double Max { get; set; }

        private List<HeatMapMethodAttribute> _methods = new List<HeatMapMethodAttribute>();

        public byte GetIntensity(Simulations.Environment environment, int x, int y)
        {
            return GetIntensityByFlagState(environment, x, y);
        }

        public List<HeatMapMethodAttribute> GetIntensityMethods()
        {
            if (_methods == null)
            {
                this.CacheHeatmapMethods(out var heatMethods, out this._methods);
            }
            return _methods;
        }

        public void SetMethod(int i, out double range_start, out double range_end)
        {
            throw new NotImplementedException();
        }

        [HeatMapMethodAttribute (max = 1, min = 0, name = "Base")]
        public byte GetIntensityByFlagState(Simulations.Environment environment, int x, int y)
        {
            if (!environment.IsOpen(x, y)) return 0;
            if (environment.GetFlag(x, y, Simulations.Environment.PointType.FIXED)) return 100;
            return 254;
        }
    }
}
