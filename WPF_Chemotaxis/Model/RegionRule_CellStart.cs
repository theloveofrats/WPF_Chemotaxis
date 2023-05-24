using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using WPF_Chemotaxis.Simulations;
using WPF_Chemotaxis.UX;
using Newtonsoft.Json;

namespace WPF_Chemotaxis.Model
{
    public class RegionRule_CellStart : RegionRule
    {
        [JsonProperty]
        protected FieldOption<int> option1 = new FieldOption<int>();
        [JsonProperty]
        protected OptionLister<CellType> option2 = new OptionLister<CellType>(true, "Random");
        private int count
        {
            get
            {
                return option1.currentValue;
            }
        }

        public RegionRule_CellStart()
        {
            OnLoaded();
        }

        public override void OnLoaded() {
            this.DisplayName = "Cell Start";
            option1.PropertyChanged += this.FireUpdate;
            option2.PropertyChanged += this.FireUpdate;
        }


        public override void Tick(Simulation sim, ICollection<Vector2Int> points)
        {
            throw new NotImplementedException();
        }

        public override void Init(Simulation sim, ICollection<Vector2Int> points)
        {
            SciRand rnd = new SciRand();
            List<Vector2Int> regionPoints = points.ToList();

            CellType ct = option2.Selected;
            CellType[] draw = null;
            if (ct == null)
            {
                draw = option2.RandomDraw(count);
            }

            double x, y;
            Vector2Int startingPoint;
            for(int i=0; i<count; i++)
            {
                startingPoint = rnd.RandomElement(regionPoints);
                x = startingPoint.X * sim.Environment.settings.DX;
                y = startingPoint.Y * sim.Environment.settings.DX;

                if (ct == null)
                {
                    sim.AddCell(draw[i], x,y, CellEventType.JUST_APPEARED);
                }
                else
                {
                    sim.AddCell(ct,x,y, CellEventType.JUST_APPEARED);
                }
            }
        }

        public override FlowDocument Document
        {
            get
            {
                //Needs refreshing
                option2.GenerateOptions(Model.MasterElementList);

                FlowDocument fd = new FlowDocument();
                Paragraph p = new Paragraph();
                TextBlock tb;
                
                tb = new TextBlock();
                tb.Text = count.ToString()+" ";
                tb.FontWeight = FontWeights.Bold;
                tb.MouseLeftButtonDown += (s, e) => option1.OpenPropField(e);
                p.Inlines.Add(tb);

                tb = new TextBlock();
                tb.Text = option2.SelectedLabel;
                tb.FontWeight = FontWeights.Bold;
                tb.MouseLeftButtonDown += (s, e) => option2.ShowOptionsPopup(e);
                p.Inlines.Add(tb);

                tb = new TextBlock();
                tb.Text = " cells added";

                p.Inlines.Add(tb);

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
