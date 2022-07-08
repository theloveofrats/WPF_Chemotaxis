using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Chemotaxis;
using WPF_Chemotaxis.Model;
using WPF_Chemotaxis.UX;
using Newtonsoft.Json;
using WPF_Chemotaxis.Simulations;

namespace ECM_Plugin
{
    internal class ECM_Ligand_Interaction : LabelledLinkable
    {
        [JsonPropertyAttribute]
        [LinkAttribute]
        private Ligand ligand;
        public Ligand Ligand
        {
            get
            {
                return ligand;
            }
        }

        [JsonPropertyAttribute]
        [LinkAttribute]
        private ECMType ecm;
        public ECMType ECM
        {
            get
            {
                return ecm;
            }
        }
        
        [Param(Name = "On rate (uM-1 s-1)")]
        public double Kon { get; set; } = 0.0005;

        [Param(Name = "Off rate (s-1)")]
        public double Koff { get; set; } = 0.0002;

        public double Kd // (uM)
        {
            get
            {
                return Koff / Kon;
            }
        }

        private Dictionary<int, double> stored = new();
        public double BoundLigandConcentration(int index)
        {
            if (stored.ContainsKey(index))
            {
                return (stored[index]);
            }
            // CHANGE THIS YOU FOOL
            else return 1E10;
        }

        public ECM_Ligand_Interaction(ECMType ecm, Ligand ligand)
        {
            this.ecm = ecm;
            this.ligand = ligand;
        }



        public void InitialiseIndex(int index)
        {
            this.stored.Add(index, 0.0);
        }

        public double CalculateSiteReaction(int index, Simulation sim)
        {
            int x = index % sim.W;
            int y = index / sim.W;

            double cA = this.ECM.UnboundSiteConcentration(index);
            double cB = sim.Environment.GetConcentration(this.Ligand, x, y);
            double cC = this.BoundLigandConcentration(index);

            double rate = cA * cB * Kon - cC * Koff;

            /*if (rate > 0)
            {
                if (rate > cA / sim.Settings.dt) rate = cA / sim.Settings.dt;
                if (rate > cB / sim.Settings.dt) rate = cB / sim.Settings.dt;
            }
            if (rate < 0)
            {
                if (rate < cC / sim.Settings.dt) rate = cC / sim.Settings.dt;
            }*/

            this.stored[index] += rate * sim.Settings.dt;

            if (this.stored[index] > 0)
            {
                //System.Diagnostics.Debug.WriteLine(string.Format("Stored {0:0.00} of ligand {1}", stored[index], this.ligand.Name));
            }

            return rate;        
        }
    }
}
