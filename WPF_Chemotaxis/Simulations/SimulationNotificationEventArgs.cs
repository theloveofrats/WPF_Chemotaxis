using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Chemotaxis.Simulations
{

    public class SimulationNotificationEventArgs
    {
        public double dt { get; private set; }
        public SimulationNotificationEventArgs(double dt)
        {
            this.dt = dt;
        }
    }
}
