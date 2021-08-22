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
    public class RegionRule_LigandStart : RegionRule
    {
        [JsonProperty]
        private FieldOption<double> option1 = new FieldOption<double>();
        [JsonProperty]
        private OptionLister<Ligand> option2 = new OptionLister<Ligand>(false,"");

        public RegionRule_LigandStart()
        {
            OnLoaded();
        }

        public override void Tick(Simulation sim, ICollection<Vector2Int> points)
        {
            throw new NotImplementedException();
        }

        public override void Init(Simulation sim, ICollection<Vector2Int> points)
        {      
            foreach(Vector2Int point in points)
            {
                sim.Environment.SetConcentration(point.X, point.Y, option2.Selected, option1.currentValue);
            }
            System.Diagnostics.Debug.Print("Setting ligand values");
        }

        public override void OnLoaded()
        {
            this.DisplayName = "Ligand conc.";
            option1.PropertyChanged += FireUpdate;
            option2.PropertyChanged += FireUpdate;
        }

        public override FlowDocument Document
        {
            get
            {
                
                FlowDocument fd = new FlowDocument();
                Paragraph p = new Paragraph();
                TextBlock tb;
                option2.GenerateOptions(Model.MasterElementList);

                tb = new TextBlock();

                tb.Text = "Start with ";
                p.Inlines.Add(tb);

                tb = new TextBlock();
                tb.Text = option1.currentValue.ToString();
                tb.FontWeight = FontWeights.Bold;
                tb.MouseDown += (s, e) => option1.OpenPropField(e);
                p.Inlines.Add(tb);

                tb = new TextBlock();
                tb.Text = "uM ";
                p.Inlines.Add(tb);

                tb = new TextBlock();
                tb.Text = option2.SelectedLabel;
                tb.FontWeight = FontWeights.Bold;
                tb.MouseLeftButtonDown += (s, e) => option2.ShowOptionsPopup(e);
                // button code beeded here!
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