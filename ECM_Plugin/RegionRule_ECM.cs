using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using WPF_Chemotaxis;
using WPF_Chemotaxis.Model;
using WPF_Chemotaxis.Simulations;

namespace ECM_Plugin
{
    public class RegionRule_ECM : RegionRule
    {
        [JsonProperty]
        private OptionLister<ECMType> optionECM = new OptionLister<ECMType>(false, "");

        public RegionRule_ECM()
        {
            this.DisplayName = "Contains ECM";
        }

        public override void Tick(Simulation sim, ICollection<Vector2Int> points)
        {
            if (optionECM.Selected == null) return;
            optionECM.Selected.Update(sim, points);
        }

        public override void Init(Simulation sim, ICollection<Vector2Int> points)
        {
            if (optionECM.Selected == null) return;
            foreach (Vector2Int point in points)
            {
                optionECM.Selected.InitECMPoint(sim, point.X, point.Y);
            }
            hasTick = true;
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
                TextBlock tb;

                optionECM.GenerateOptions(Model.MasterElementList);

                tb = new TextBlock();

                tb.Text = "Contains ";
                p.Inlines.Add(tb);

                tb = new TextBlock();
                tb.Text = optionECM.SelectedLabel;
                tb.FontWeight = FontWeights.Bold;
                tb.MouseLeftButtonDown += (s, e) => optionECM.ShowOptionsPopup(e);
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
