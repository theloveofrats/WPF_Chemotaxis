using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using WPF_Chemotaxis.UX;
using WPF_Chemotaxis.VisualScripting;

namespace WPF_Chemotaxis.Model
{
    /// <summary>
    /// The class that defines the parameters of a given ligand type in
    /// the model. Parameters are exposed to the UI for modification.
    /// </summary>
    
    [VSElementAttribute(ui_TypeLabel = "Ligand", symbolResourcePath = "Resources/LigandIcon.png", symbolSize = 6.0)]
    public class Ligand : LabelledLinkable, IHeatMapSource
    {
        public string label = "Ligand";

        [Param(Name = "Diffusivity (um^2/min)")]
        public double Diffusivity { get; set; } = 10000;

        [Param(Name = "Feed rate (uM/um^2/min)")]
        //private double env_expr_rate;
        public double FeedRate { get; set; } = 0;

        [Param(Name = "Kill rate (uM/um^2/min)")]
        public double KillRate { get; set; } = 0;

        [LinkAttribute]
        public List<LigandReceptorRelation> receptorInteractions = new();

        public Ligand() : base() { }
        public Ligand(string label) : base(label) { }




        //Specifically the parts of the class that are IHeatMapSource related.
        private List<Func<Simulations.Environment, int, int, byte>> heatMethods;
        private List<HeatMapMethodAttribute> heatMethodTags;
        private double min;
        private double max;
        public double Min
        {
            get
            {
                return min;
            }
            set
            {
                min = value;
            }
        }
        public double Max
        {
            get
            {
                return max;
            }
            set
            {
                max = value;
            }
        }

        public override void RemoveElement(ILinkable element, ILinkable replacement=null)
        {
            if(element is LigandReceptorRelation)
            {
                LigandReceptorRelation lrr = (LigandReceptorRelation) element;
                if (this.receptorInteractions.Contains(lrr))
                {
                    this.receptorInteractions.Remove(lrr);
                    if (replacement != null && replacement.GetType().IsAssignableTo(typeof(LigandReceptorRelation)))
                    {
                        this.receptorInteractions.Add((LigandReceptorRelation)replacement);
                    }
                }
            }
        }

        private Func<Simulations.Environment, int, int, byte> currentIntensityMethod;
        public byte GetIntensity(Simulations.Environment environment, int x, int y)
        {
            if (currentIntensityMethod == null)
            {
                currentIntensityMethod = heatMethods[0];
            }
            return currentIntensityMethod(environment, x, y);
        }

        public List<HeatMapMethodAttribute> GetIntensityMethods()
        {
            if(heatMethods == null)
            {
                this.CacheHeatmapMethods(out this.heatMethods, out this.heatMethodTags);
            }
            return heatMethodTags;
        }

        public void SetMethod(int i, out double minval, out double maxval)
        {
            if (i < 0)
            {
                minval = 0;
                maxval = 0;
                return;
            }
            currentIntensityMethod = heatMethods[i];
            Min = heatMethodTags[i].min;
            Max = heatMethodTags[i].max;

            minval = Min;
            maxval = Max;
        }

        [HeatMapMethod(name ="Concentration", min = 0, max = 10)]
        public byte ConcentrationToIntensity(Simulations.Environment environment, int x, int y)
        {
            double c = Math.Clamp(environment.GetConcentration(this, x, y), min, max);

            c = 255 * (c - min) / (max - min);

            return (byte) Math.Round(c);
        }

        [HeatMapMethod(name = "Steepness", min = -0.05, max = 0.05)]
        public byte SteepnessToIntensity(Simulations.Environment environment, int x, int y)
        {
            double c, n, e;
            c = environment.GetConcentration(this, x, y);
            n = environment.GetConcentration(this, x, y+1);
            e = environment.GetConcentration(this, x+1, y);

            c = Math.Clamp(0.5 * (2 * c - (n + e))/environment.settings.DX, min, max);

            c = 255 * (c - min) / (max - min);

            return (byte)Math.Round(c);
        }
    }
}
