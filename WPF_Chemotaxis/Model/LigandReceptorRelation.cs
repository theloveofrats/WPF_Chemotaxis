using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Chemotaxis.UX;
using Newtonsoft.Json;
using WPF_Chemotaxis.VisualScripting;

namespace WPF_Chemotaxis.Model
{
    /// <summary>
    /// Defines the relationship between a given ligand and a given receptor type. 
    /// Parameters fro kD and efficacy are exposed to the UI for modification.
    /// </summary>
    [VSRelationAttribute(forcedPositionType = ForcedPositionType.NONE, childFieldName = "receptor", parentFieldName = "ligand")]
    public class LigandReceptorRelation : LabelledLinkable
    {
        [JsonPropertyAttribute]
        [LinkAttribute]
        private Receptor receptor;
        public Receptor Receptor
        {
            get
            {
                return receptor;
            }
        }
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

        [Param(Name = "kD (uM)")]
        public double kD { get; set;} = 0.01;
        [Param(Name = "Efficacy (0 to 1)")]
        public double eff { get; set; } = 1;

        public LigandReceptorRelation() : base() { }
        public LigandReceptorRelation(string label) : base(label) { }

        public LigandReceptorRelation(Ligand ligand,Receptor receptor) : base() {

            this.ligand = ligand;
            this.receptor = receptor;

            this.ligand.receptorInteractions.Add(this);
            this.receptor.ligandInteractions.Add(this);
            Init();
        }

        public void SetReceptor(Receptor r)
        {
            this.receptor = r;
            if (!this.receptor.ligandInteractions.Contains(this))
            {
                this.receptor.ligandInteractions.Add(this);
            }
        }
        public void SetLigand(Ligand l)
        {
            this.ligand = l;
            if (!this.ligand.receptorInteractions.Contains(this))
            {
                this.ligand.receptorInteractions.Add(this);
            }

        }

        public override void RemoveElement(ILinkable element, ILinkable replacement=null)
        {
            if(element is Receptor)
            {
                Receptor r = (Receptor)element;
                if (this.receptor == r)
                {
                    this.receptor = (Receptor) replacement;
                    if(this.receptor==null) Model.Current.RemoveElement(this);
                }
            }
            else if (element is Ligand)
            {
                Ligand l = (Ligand)element;
                if (this.ligand == l)
                {
                    this.ligand = (Ligand)replacement;
                    if(this.ligand==null) Model.Current.RemoveElement(this);
                }
            }
        }

        [JsonIgnore]
        public override string Name
        {
            get
            {
                if (receptor == null || ligand == null) return "Broken Ligand Receptor Link";
                return string.Format("{0}<->{1} interactions",receptor.Name,ligand.Name);
            }
            set
            {
                return;
            }
        }
    }
}
