using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WPF_Chemotaxis.Simulations;
using LiveCharts;
using LiveCharts.Wpf;
using System.Windows.Media;

namespace WPF_Chemotaxis
{
    interface IGraphOnSelection
    {
        public bool IsAtPosition(double x, double y);

        public void DrawHighlight(Simulation sim, WriteableBitmap bmp, Color clr);

        public LineSeries GetValues();
    }
}
