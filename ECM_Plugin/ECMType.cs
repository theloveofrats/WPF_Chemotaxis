using WPF_Chemotaxis;
using WPF_Chemotaxis.Model;
using WPF_Chemotaxis.Simulations;
using WPF_Chemotaxis.UX;

namespace ECM_Plugin
{
    [CustomBaseElementAttribute(newElementButtonLabel = "new ECM")]
    class ECMType : LabelledLinkable, IHeatMapSource
    {
        public string label = "ECM";

        [Param(Name = "GAG Saturation point (uM)")]
        public double saturation { get; set; } = 4;

        [LinkAttribute]
        public List<ECM_Ligand_Interaction> ligandInteractions = new();

        public ECMType() : base() 
        {
            Init();
        }
        public ECMType(string label) : base(label) 
        {
            Init();
        }

        //Specifically the parts of the class that are IHeatMapSource related.
        private List<Func<WPF_Chemotaxis.Simulations.Environment, int, int, byte>> heatMethods;
        private List<HeatMapMethodAttribute> heatMethodTags;
        private Func<WPF_Chemotaxis.Simulations.Environment, int, int, byte> currentIntensityMethod; 
        private double min = 0;
        private double max = 4;
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

        public double UnboundSiteConcentration(int index)
        {
            double ans = this.saturation;

            foreach (ECM_Ligand_Interaction interaction in ligandInteractions)
            {
                ans-=interaction.BoundLigandConcentration(index);
            }
            return Math.Max(0,ans);
        }

        public void InitECMPoint(Simulation sim, int x, int y)
        {
            int index = y * sim.W + x;
            foreach(ECM_Ligand_Interaction interaction in this.ligandInteractions)
            {
                interaction.InitialiseIndex(index);
            }
        }

        public void Update(Simulation sim, ICollection<Vector2Int> points)
        {
            //System.Diagnostics.Debug.WriteLine("UPDATE CALLED ON ECM");
            int index;
            foreach(var point in points)
            {
                index = point.Y * sim.W + point.X;
                CalculateSiteReaction(index, sim);
            }
        }


        [ElementAdder(label = "Add ECM-Ligand Interaction", type = typeof(Ligand))]
        public void AddLigandInteraction(Ligand ligand)
        {
            ECM_Ligand_Interaction interaction = new ECM_Ligand_Interaction(this, ligand);
            if (!ligandInteractions.Contains(interaction)) ligandInteractions.Add(interaction);
        }



        public void CalculateSiteReaction(int index, Simulation sim)
        {
            //System.Diagnostics.Debug.WriteLine("CALC CALLED ON ECM");
            int x = index % sim.W;
            int y = index / sim.W;
            double dx = sim.Environment.settings.DX;

            double rate;

            foreach(ECM_Ligand_Interaction interaction in ligandInteractions)
            {
                rate = interaction.CalculateSiteReaction(index, sim);
                sim.Environment.DegradeAtRate(interaction.Ligand, null, x * dx, y * dx,rate, 1, sim.Settings.dt);
            }
        }

        public byte GetIntensity(WPF_Chemotaxis.Simulations.Environment environment, int x, int y)
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

        [HeatMapMethod(name = "Free Sites", min = 0, max = 4)]
        public byte FreeSiteConcentrationToIntensity(WPF_Chemotaxis.Simulations.Environment environment, int x, int y)
        {
            int index = y * environment.Width + x;
            

            double c = Math.Clamp(UnboundSiteConcentration(index), min, max);

            c = 255 * (c - min) / (max - min);

            return (byte)Math.Round(c);
        }
    }
}