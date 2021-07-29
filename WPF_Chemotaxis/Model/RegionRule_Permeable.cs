using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using WPF_Chemotaxis.Simulations;

namespace WPF_Chemotaxis.Model
{
    public class RegionRule_Permeable : RegionRule
    {

       
        public RegionRule_Permeable()
        {
            this.DisplayName = "Permeable wall";
        }

        public override void Tick(Simulation sim, ICollection<Vector2Int> points)
        {
            throw new NotImplementedException();
        }

        public override void Init(Simulation sim, ICollection<Vector2Int> points)
        {
            foreach(Vector2Int point in points){
                sim.Environment.SetFlag(point.X, point.Y, Simulations.Environment.PointType.BLOCK, true);
            }
        }

        public override void OnLoaded()
        {
            
        }

        public override FlowDocument Document
        {
            get
            {
                FlowDocument fd = new FlowDocument();
                Paragraph p = new Paragraph();
                TextBlock block = new TextBlock();
                block.Text = "Blocks cells, allows diffusion";

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
