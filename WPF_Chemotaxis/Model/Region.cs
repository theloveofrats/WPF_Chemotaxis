using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using WPF_Chemotaxis.Simulations;

namespace WPF_Chemotaxis.Model
{
    public class Region
    {
        private RegionType regionType;
        private ICollection<Vector2Int> points;

        public Region(RegionType regionType, ICollection<Vector2Int> points)
        {
            this.regionType = regionType;
            this.points = points;
        }

        public void Init(Simulation sim, Simulations.Environment env)
        {
            foreach(RegionRule rule in this.regionType.Rules)
            {
                rule.Init(sim, this.points);
                if (rule.hasTick)
                {
                    sim.EarlyUpdate += (s, env, e) => rule.Tick(s, this.points);
                }
            }
        }

        public void OnClick(MouseButtonEventArgs e, Simulations.Environment environment)
        {
            foreach(RegionRule rule in this.regionType.Rules)
            {
                rule.OnClicked(e, environment, this.points);
            }
        }

        public void Draw(WriteableBitmap bmp, Simulations.Environment environment)
        {
            foreach(RegionRule rule in this.regionType.Rules)
            {
                rule.Draw(bmp, environment, this.points);
            }
        }

        public bool Contains(Vector2Int position)
        {
            return points != null && points.Contains(position);
        }

        public override string ToString()
        {
            return string.Format("{0} points of type {1}",points.Count, regionType.Name);
        }
    }
}
