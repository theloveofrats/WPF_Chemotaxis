using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Chemotaxis.UX;
using Newtonsoft.Json;
using WPF_Chemotaxis.VisualScripting;
using System.Windows.Media;

namespace WPF_Chemotaxis.Model
{
    /// <summary>
    /// Defines the relationship between a given ligand and a given receptor type. 
    /// Parameters fro kD and efficacy are exposed to the UI for modification.
    /// </summary>
    public class LigandRelation : LabelledLinkable
    {
        [VisualLine(parentAnchor = LineAnchorType.ANCHOR_FORWARD, childAnchor = LineAnchorType.ANCHOR_CENTRE, parentAnchorDistance = 25.0, childAnchorDistance = 10.0, parentArrowHeadFunc = "GetArrowhead", colorFunc = "EfficacyColor")]
        [JsonProperty]
        [Link]
        public Ligand Ligand { get; private set; }

        [Param(Name = "kD (uM)")]
        public double kD { get; set;} = 0.01;
        [Param(Name = "Efficacy (0 to 1)")]
        public double eff { get; set; } = 1;
        [Param(Name = "Inhibitor molecule")]
        public bool Inhibitor { get; set; } = false;
        [Param(Name = "Uncompetitive")]
        public bool Uncompetitive { get; set; } = false;
 
        public LigandRelation() : base() 
        {

        }
        public LigandRelation(string label) : base(label) { }

        public LigandRelation(Ligand ligand) : base() {

            this.Ligand = ligand;
        }

        public void SetLigand(Ligand l)
        {
            this.Ligand = l;
            /*if (!this.Ligand.receptorInteractions.Contains(this))
            {
                this.Ligand.receptorInteractions.Add(this);
            }*/
        }
        private Color ColorLerp(Color clr1, Color clr2, double val)
        {
            val = Math.Clamp(val, 0, 1);
            byte R = (byte) (clr1.R*(1.0-val) + clr2.R*val);
            byte G = (byte)(clr1.G * (1.0 - val) + clr2.G * val);
            byte B = (byte)(clr1.B * (1.0 - val) + clr2.B * val);
            byte A = (byte)(clr1.A * (1.0 - val) + clr2.A * val);

            Color clrOut = new Color();
            clrOut.R = R; 
            clrOut.G = G;
            clrOut.B = B;
            clrOut.A = A;

            return clrOut;
        }

        private Color ColorFromGradient(GradientStopCollection palette, double gradValue)
        {
            var sorted = palette.OrderBy(gs => gs.Offset).ToList();

            if (gradValue <= sorted[0].Offset) return sorted[0].Color;
            if (gradValue >= sorted[sorted.Count()-1].Offset) return sorted[sorted.Count() - 1].Color;

            GradientStop gs1, gs2;

            for (int i= 0; i < sorted.Count() - 1; i++)
            {
                gs1 = sorted[i];
                gs2 = sorted[i + 1];
                if (gradValue >= gs1.Offset && gradValue < gs2.Offset)
                {
                    double lerpval = (gradValue-gs1.Offset) / (gs2.Offset - gs1.Offset);
                    return ColorLerp(gs1.Color, gs2.Color, lerpval);
                }
            }
            return sorted[sorted.Count() - 1].Color;
        }

        protected LineHeadType GetArrowhead()
        {
            if (Inhibitor) return LineHeadType.INHIBIT;
            return LineHeadType.CIRCLE;
        }

        protected Color EfficacyColor()
        {
            if (Inhibitor)
            {
                System.Diagnostics.Debug.Print("LIGAND IS INHIBITOR");
                return Colors.Red;
            }
            else
            {
                System.Diagnostics.Debug.Print(this.Name+"  APPARENTLY NOT INHIBITOR");
            }

            GradientStopCollection gsc = new GradientStopCollection(){
               new GradientStop(Colors.Blue, 1),
               new GradientStop(Colors.Blue, 0.9),
               new GradientStop(Colors.Gray, 0.5),
               new GradientStop(Colors.Red, 0.1),
               new GradientStop(Colors.Red, 0.0) 
            };

            return ColorFromGradient(gsc, this.eff);
        }

        public override void RemoveElement(ILinkable element, ILinkable replacement=null)
        {
            if (element is Ligand)
            {
                Ligand l = (Ligand)element;
                if (this.Ligand == l)
                {
                    this.Ligand = (Ligand)replacement;
                    if(this.Ligand == null) Model.Current.RemoveElement(this);
                }
            }
        }

        [JsonIgnore]
        public override string Name
        {
            get
            {
                if (Ligand == null) return "Broken Ligand Receptor Link";
                return string.Format("{0} interaction", Ligand.Name);
            }
            set
            {
                return;
            }
        }
    }
}
