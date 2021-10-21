using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Chemotaxis.UX;

namespace WPF_Chemotaxis.Model
{
    /// <summary>
    /// Representation of a receptor type in the theoretical model. Links to some number of ligands. During a simulation, 
    /// each receptor will take input ligand concentations from a cell and respond with levels of occupancy and activity.
    /// </summary>
    public class Receptor : LabelledLinkable, IHeatMapSource
    {
        public string label = "Receptor";

        [LinkAttribute]
        public List<LigandReceptorRelation> ligandInteractions = new();

        public Receptor() : base()
        {

        }
        
        public Receptor(string label) : base(label)
        {

        }

        [ElementAdder(label = "Add Ligand", type = typeof(Ligand))]
        public void AddLigand(Ligand ligand)
        {
            foreach(LigandReceptorRelation inter in ligandInteractions)
            {
                if (inter.Ligand.Equals(ligand)) return;
            }
            new LigandReceptorRelation(ligand, this);
        }

        public override void RemoveElement(ILinkable element)
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
                    this.ligandInteractions.Remove(found);
                }
            }
            else if (element is LigandReceptorRelation)
            {
                LigandReceptorRelation lrr = (LigandReceptorRelation)element;
                if (this.ligandInteractions.Contains(lrr)) this.ligandInteractions.Remove(lrr);
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

            double affinityPart;
            foreach (LigandReceptorRelation lrr in this.ligandInteractions)
            {
                affinityPart = environment.GetConcentration(lrr.Ligand, x, y) / lrr.kD;
                ckd_btm += affinityPart;
                ckd_top += affinityPart * lrr.eff;
            }

            return ckd_top / (ckd_btm + 1.0);
        }
        #endregion .  IHeatmapSource Drawing methods
    }
}
