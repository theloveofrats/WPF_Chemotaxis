using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Chemotaxis.UX;
using WPF_Chemotaxis.Simulations;
using System.Windows;
using Newtonsoft.Json;

namespace WPF_Chemotaxis.Model
{
    /// <summary>
    /// Relational class for cell-ligand direct interactions (rather than receptor-mediated interactions).
    /// </summary>
    public class CellLigandRelation : LabelledLinkable
    {
        [JsonProperty]
        [Link]
        CellType cellType;
        
        [JsonProperty]
        [InstanceChooser(label = "Input ligand")]
        Ligand input_ligand;
        [JsonProperty]
        [InstanceChooser(label = "Output ligand")]
        Ligand output_ligand;
        [JsonProperty]
        [InstanceChooser(label = "Activator/antagonist")]
        Ligand extra_ligand;

        //DEBUG REMOVE!
        Random random = new Random();

        [Param(Name = "I/O ratio")]
        public double multiplier { get; set; } = 1;
        [Param(Name = "vMax")]
        public double vMax { get; set; } = 10;
        [Param(Name = "Hill Coeficient")]
        public double Hill { get; set; } = 1;
        [Param(Name = "kM")]
        public double kM { get; set; } = 0.5;
        [Param(Name = "Antagonist kD")]
        public double akD { get; set; } = 0.5;


        public CellLigandRelation() { }

        public CellLigandRelation(CellType cell, Ligand ligand) : base()
        {
            this.cellType = cell;
            this.input_ligand = ligand;
        }

        public override void RemoveElement(ILinkable element, ILinkable replacement=null)
        {
            if (element is Ligand)
            {
                Ligand l = (Ligand)element;
                if (this.input_ligand == l)
                {
                    this.input_ligand = (Ligand) replacement;
                }
                if(this.output_ligand == l)
                {
                    this.output_ligand = (Ligand) replacement;
                }
                if (this.extra_ligand == l)
                {
                    this.extra_ligand = (Ligand) replacement;
                }
                if(this.input_ligand==null && this.output_ligand == null)
                {
                    Model.Current.RemoveElement(this);
                }
            }
            else if (element is CellType)
            {
                CellType ct = (CellType)element;
                if (this.cellType == ct)
                {
                    this.cellType = (CellType) replacement;

                    if(this.cellType==null) Model.Current.RemoveElement(this);
                }
            }
        }

        public void DoUpdateAction(Simulations.Environment env, Cell c, double dt)
        {
            foreach (Point p in c.localPoints) {

                double rate = vMax/c.localPoints.Count;

                //Offload to a proper solver later!
                if (input_ligand != null)
                {
                    double in_cnc = env.GetConcentration(input_ligand, p.X, p.Y);

                   

                    if (Hill == 1) {
                        rate *= in_cnc / (in_cnc + kM);
                    }
                    else
                    {
                        double fc = Math.Pow(in_cnc, Hill);
                        rate *= fc / (fc + Math.Pow(kM, Hill));
                    }
                }

                env.DegradeAtRate(input_ligand, output_ligand, p.X, p.Y, rate, multiplier, dt);
           
            }
        }

        private Ligand FirstLigand
        {
            get
            {
                if (input_ligand != null) return input_ligand;
                if (output_ligand != null) return output_ligand;
                if (extra_ligand != null) return extra_ligand;
                return null;
            }
        }

        [JsonProperty]
        private string name;
        [JsonIgnore]
        public override string Name
        {
            get
            {
                if (name != null) return name;
                else if(cellType == null || (FirstLigand==null && output_ligand==null)) return "Broken Cell-Ligand interaction";
                else if (FirstLigand==null) return string.Format("New {0}-{1} interaction", cellType.Name, output_ligand.Name);
                return string.Format("New {0}-{1} interaction", cellType.Name, FirstLigand.Name);
            }
            set
            {
                name = value;
            }
        }
    }
}
