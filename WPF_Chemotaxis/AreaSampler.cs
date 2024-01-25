using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WPF_Chemotaxis.Simulations;

namespace WPF_Chemotaxis
{
    class AreaSampler : IGraphOnSelection
    {
        private Rect rect;
        private Simulation sim;
        private LineSeries series = new()
        {
            Values = new ChartValues<ObservablePoint>(),
            LineSmoothness = 0.25,
            Fill=Brushes.Transparent,
            ToolTip=null,
            
        };

        public AreaSampler(Simulation sim, double x, double y, double w, double h)
        {
            this.sim = sim;
            this.rect = new Rect(x, y, w, h);
        }

        public bool IsAtPosition(double x, double y)
        {
            return rect.Contains(x, y);
        }

        public void DrawHighlight(Simulation sim, WriteableBitmap bmp, Color clr)
        {
            double dx = sim.Environment.settings.DX;
            double scale = bmp.PixelWidth * 1.0 / (dx * sim.Environment.Width);

            WriteableBitmapExtensions.DrawRectangle(bmp, (int)Math.Round(scale * rect.X), (int)Math.Round(scale * rect.Y), (int)Math.Round(scale*(rect.X+rect.Width)), (int)Math.Round(scale * (rect.Y+rect.Height)), clr);
        }

        private double GetCellCount()
        {
            // TODO Trash. Change this for, I guess, something associated with the view window, that catches its heatmap output?
            double count = 0;
            foreach(Cell c in sim.Cells)
            {
                if(rect.Contains(c.X, c.Y))
                {
                    count += 1;
                }
            }
            return count;
        }

        public LineSeries GetValues()
        {
            series.Values.Add(new ObservablePoint(sim.Time, GetCellCount()));
            return series;
        }

        public Rect Bounds
        {
            get
            {
                return this.rect;
            }
        }
    }
}
