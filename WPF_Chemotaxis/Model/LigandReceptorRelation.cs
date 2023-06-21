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
    [VSRelationAttribute(forcedPositionType = ForcedPositionType.NONE, childFieldName = "_ligand", parentFieldName = "_receptor")]
    public class LigandReceptorRelation : LabelledLinkable
    {
        [JsonPropertyAttribute]
        [LinkAttribute]
        private Receptor _receptor;
        public Receptor Receptor
        {
            get
            {
                return _receptor;
            }
        }

        [VisualLine(lineType = LineType.LINE)]
        [JsonPropertyAttribute]
        [LinkAttribute] 
        private Ligand _ligand;
        public Ligand Ligand
        {
            get
            {
                return _ligand;
            }
        }

        [Param(Name = "kD (uM)")]
        public double kD { get; set;} = 0.01;
        [Param(Name = "Efficacy (0 to 1)")]
        public double eff { get; set; } = 1;

        public LigandReceptorRelation() : base() 
        {
            Init();
        }
        public LigandReceptorRelation(string label) : base(label) { }

        public LigandReceptorRelation(Ligand ligand,Receptor receptor) : base() {

            this._ligand = ligand;
            this._receptor = receptor;

            this._ligand.receptorInteractions.Add(this);
            this._receptor.ligandInteractions.Add(this);
            Init();
        }

        public void SetReceptor(Receptor r)
        {
            this._receptor = r;
            if (!this._receptor.ligandInteractions.Contains(this))
            {
                this._receptor.ligandInteractions.Add(this);
            }
        }
        public void SetLigand(Ligand l)
        {
            this._ligand = l;
            if (!this._ligand.receptorInteractions.Contains(this))
            {
                this._ligand.receptorInteractions.Add(this);
            }

        }

        public override void RemoveElement(ILinkable element, ILinkable replacement=null)
        {
            if(element is Receptor)
            {
                Receptor r = (Receptor)element;
                if (this._receptor == r)
                {
                    this._receptor = (Receptor) replacement;
                    if(this._receptor==null) Model.Current.RemoveElement(this);
                }
            }
            else if (element is Ligand)
            {
                Ligand l = (Ligand)element;
                if (this._ligand == l)
                {
                    this._ligand = (Ligand)replacement;
                    if(this._ligand==null) Model.Current.RemoveElement(this);
                }
            }
        }

        [JsonIgnore]
        public override string Name
        {
            get
            {
                if (_receptor == null || _ligand == null) return "Broken Ligand Receptor Link";
                return string.Format("{0}<->{1} interactions",_receptor.Name,_ligand.Name);
            }
            set
            {
                return;
            }
        }
    }
}
