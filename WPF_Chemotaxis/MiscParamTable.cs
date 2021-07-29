using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Chemotaxis
{
    public struct MiscParamTable
    {
        public double dx { get; private set; }
        public double duration { get; private set; }
        public double dt { get; private set; }
        public double saveInterval { get; private set; }

        public MiscParamTable(double dx, double duration, double dt, double saveInterval)
        {
            this.dx = dx;
            this.duration = duration;
            this.dt = dt;
            this.saveInterval = saveInterval;
        }
    }
}
