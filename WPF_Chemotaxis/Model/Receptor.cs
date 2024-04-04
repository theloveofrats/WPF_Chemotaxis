using Newtonsoft.Json;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Chemotaxis.UX;
using WPF_Chemotaxis.VisualScripting;

namespace WPF_Chemotaxis.Model
{
    /// <summary>
    /// Representation of a receptor type in the theoretical model. Links to some number of ligands. During a simulation, 
    /// each receptor will take input ligand concentations from a cell and respond with levels of occupancy and activity.
    /// </summary>
    [VSElementAttribute(ui_TypeLabel = "Receptor", symbolResourcePath = "Resources/ReceptorIcon.png", symbolSize = 6.0, tagX = 12, tagY = -12, tagCentre = false)]
    public class Receptor : LabelledLinkable, IHeatMapSource
    {
        public string label = "Receptor";

        public static HashSet<string> AllReceptorClasses { get; private set; } = new();

        [JsonProperty]
        protected string _receptorClass = string.Empty;

        [Param(Name = "Receptor type (e.g. GPCR, toll-like)")]
        [DoNotCheckEquality]
        public string ReceptorClass {
            get
            {
                Debug.WriteLine("Getting current receptor class value.");
                return _receptorClass;
            }
            set 
            {
                Debug.WriteLine(string.Format("Submitted RC value {0}", value));
                if (value!=string.Empty) AllReceptorClasses.Add(value);
                _receptorClass = value;
                Debug.WriteLine("The following receptor classes exist:");
                foreach(string rc in AllReceptorClasses)
                {
                    Debug.WriteLine(rc);
                }
            }
        }

        [Link]
        public List<LigandReceptorRelation> ligandInteractions { get; private set; } = new();

        public Receptor() : base()
        {
            Init();
        }
        
        public Receptor(string label) : base(label)
        {
            Init();
        }

        protected override void Init()
        {
            Debug.WriteLine(string.Format("Running derived Receptor Init() for receptor {0} with _receptorClass {1}", Name, _receptorClass));
            ReceptorClass = _receptorClass;
            base.Init();
        }

        [ElementAdder(label = "Add Ligand", type = typeof(Ligand))]
        public void AddLigand(Ligand ligand)
        {
            foreach(LigandReceptorRelation inter in ligandInteractions)
            {
                if (inter.Ligand==ligand) return;
            }
            new LigandReceptorRelation(ligand, this);
        }

        public override void RemoveElement(ILinkable element, ILinkable replacement=null)
        {
            if(element is Ligand)
            {
                Ligand lig = (Ligand)element;
                LigandReceptorRelation found = null;
                foreach(LigandReceptorRelation lrr in this.ligandInteractions)
                {
                    if (lrr.Ligand == lig)
                    {
                        found = lrr;
                        break;
                    }
                }
                if (found != null)
                {
                    if(replacement != null && replacement.GetType().IsAssignableTo(typeof(Ligand)))
                    {
                        found.SetLigand((Ligand) replacement);
                    }
                    else
                    {
                        this.ligandInteractions.Remove(found);
                    }
                }
            }
            else if (element is LigandReceptorRelation)
            {
                LigandReceptorRelation lrr = (LigandReceptorRelation)element;
                if (this.ligandInteractions.Contains(lrr))
                {
                    this.ligandInteractions.Remove(lrr);
                    if(replacement!=null && replacement.GetType().IsAssignableTo(typeof(LigandReceptorRelation)))
                    {
                        this.ligandInteractions.Add((LigandReceptorRelation) replacement);
                    }
                }
            }
        }

        //Beyond here is IHeatmapSource stuff!
        #region .  IHeatmapSource Drawing methods

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

            if (heatMethods == null)
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

        
        [HeatMapMethod(name = "Occupancy", min = 0, max = 1)]
        public byte OccupancyToIntensity(Simulations.Environment environment, int x, int y)
        {     
            double c = Math.Clamp(GetOccupancy(environment, x, y), min, max);
            c = 255 * (c - min) / (max - min);
            return (byte) Math.Round(c);
        }

        [HeatMapMethod(name = "Efficacy", min = 0, max = 1)]
        public byte EfficacyToIntensity(Simulations.Environment environment, int x, int y)
        {
            double c = Math.Clamp(GetEfficacy(environment, x, y), min, max);
            c = 255 * (c - min) / (max - min);
            return (byte)Math.Round(c);
        }

        [HeatMapMethod(name = "Occ' difference", min = -0.00025, max = 0.00025)]
        public byte DeltaOccToIntensity(Simulations.Environment environment, int x, int y)
        {
            double c, n, e;
            c = GetOccupancy(environment, x, y);
            n = GetOccupancy(environment, x, y + 1);
            e = GetOccupancy(environment, x + 1, y);

            c = Math.Clamp(0.5 * (2 * c - (n + e)), min, max);
            c = 255 * (c - min) / (max - min);
            return (byte)Math.Round(c);
        }

        [HeatMapMethod(name = "Eff' difference", min = -0.00025, max = 0.00025)]
        public byte DeltaEfficacyToIntensity(Simulations.Environment environment, int x, int y)
        {
            double c, n, e;
            c = GetEfficacy(environment, x, y);
            n = GetEfficacy(environment, x, y + 1);
            e = GetEfficacy(environment, x + 1, y);

            c = Math.Clamp(0.5 * (2 * c - (n + e)), min, max);
            c = 255 * (c - min) / (max - min);
            return (byte)Math.Round(c);
        }

        public double GetOccupancy(Simulations.Environment environment, double x, double y)
        {
            return GetOccupancy(environment, (int)(x / environment.settings.DX), (int)(y / environment.settings.DX));
        }

        public double GetOccupancy(Simulations.Environment environment, int x, int y)
        {
            double ckd_sum = 0;
            foreach (LigandReceptorRelation lrr in this.ligandInteractions)
            {
                ckd_sum += environment.GetConcentration(lrr.Ligand, x, y) / lrr.kD;
            }

            return ckd_sum / (ckd_sum + 1.0);
        }
        public double GetEfficacy(Simulations.Environment environment, double x, double y)
        {
            return GetEfficacy(environment, (int)(x / environment.settings.DX), (int)(y / environment.settings.DX));
        }

        public double GetEfficacy(Simulations.Environment environment, int x, int y)
        {
            double ckd_top = 0;
            double ckd_btm = 0;
            double noncompetitiveMultiplier = 1f;
            double uncompetitiveMultiplier = 1f;

            double affinityPart;
            foreach (LigandRelation lr in this.ligandInteractions)
            {
                if (lr.Inhibitor)
                {
                    if (lr.Uncompetitive)
                    {
                        uncompetitiveMultiplier *= 1d / (1d + (environment.GetConcentration(lr.Ligand, x, y) / lr.kD));
                    }
                    else
                    {
                        noncompetitiveMultiplier *= 1d/ (1d+ (environment.GetConcentration(lr.Ligand, x, y) / lr.kD));
                    }
                }
                else
                {
                    affinityPart = environment.GetConcentration(lr.Ligand, x, y) / lr.kD;
                    affinityPart /= uncompetitiveMultiplier;
                    ckd_btm += affinityPart;
                    ckd_top += affinityPart * lr.eff;
                }
            }

            return noncompetitiveMultiplier * uncompetitiveMultiplier * ckd_top / (ckd_btm + 1.0);
        }
        #endregion .  IHeatmapSource Drawing methods
    }
}
