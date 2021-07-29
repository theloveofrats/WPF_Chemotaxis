using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using WPF_Chemotaxis.Simulations;

namespace WPF_Chemotaxis.Model
{
    public class RegionRule_Valve : RegionRule
    {

       
        public RegionRule_Valve()
        {
            this.DisplayName = "Valve";
        }

        public override void Tick(Simulation sim, ICollection<Vector2Int> points)
        {
            throw new NotImplementedException();
        }

        public override void Init(Simulation sim, ICollection<Vector2Int> points)
        {
            //foreach(Vector2Int point in points){
            //    sim.Environment.SetFlag(point.X, point.Y, Simulations.Environment.PointType.FREE, false);
            //}
        }

        public override void OnClicked(MouseButtonEventArgs e, Simulations.Environment environment, ICollection<Vector2Int> points)
        {
           
            if (e.ChangedButton == MouseButton.Left)
            {
              
                bool val;
                foreach (Vector2Int point in points)
                {
                    val = environment.GetFlag(point.X, point.Y, Simulations.Environment.PointType.FREE);
                    environment.SetFlag(point.X, point.Y, Simulations.Environment.PointType.FREE, !val);
                }
            }
        }

        public override void OnLoaded()
        {
            
        }

        public override void Draw(WriteableBitmap targetCanvas, Simulations.Environment environment, ICollection<Vector2Int> points)
        {
            Vector2Int v = points.First();
            bool open = environment.GetFlag(v.X, v.Y, Simulations.Environment.PointType.FREE);

            Color clr;
            foreach (Vector2Int vec in points) {
                
                clr = targetCanvas.GetPixel(vec.X, vec.Y);
                if (open) {
                    clr.R = (byte)(clr.R * 0.8);
                    clr.G = (byte)(clr.G * 0.8);
                    clr.B = (byte)(clr.B * 0.8);
                }
                else
                {
                    clr.R = (byte)(clr.R * 0.2);
                    clr.G = (byte)(clr.G * 0.2);
                    clr.B = (byte)(clr.B * 0.2);
                }
                targetCanvas.SetPixel(vec.X, vec.Y, clr);
            }
        }

        public override FlowDocument Document
        {
            get
            {
                FlowDocument fd = new FlowDocument();
                Paragraph p = new Paragraph();
                TextBlock block = new TextBlock();
                block.Text = "Valve. Click to open/close";

                p.Inlines.Add(block);
                fd.Blocks.Add(p);
                return fd;
            }
            set
            {
                this.Document = value;
            }
        }
    }
}
