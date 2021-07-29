using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Chemotaxis.UX;
using Newtonsoft.Json;

namespace WPF_Chemotaxis.Model
{
    /// <summary>
    /// Class quantifying the relationship between a cell type and a receptor type.
    /// </summary>
    public class CellReceptorRelation : LabelledLinkable
    {
        [JsonProperty]
        [Link]
        private Receptor receptor;
        public Receptor Receptor { 
            get {
                return receptor;    
            } 
        }
        [JsonProperty]
        [Link]
        private CellType cell;
        public CellType Cell
        {
            get
            {
                return cell;
            }
        }

        [Param(Name = "Receptor weight", Min = 0)]
        public CenteredDoubleRange Weight { get; set; } = new CenteredDoubleRange(1,0);

        public CellReceptorRelation() : base() { }
        public CellReceptorRelation(string label) : base(label) { }

        public CellReceptorRelation(CellType cell, Receptor receptor) : base()
        {

            this.cell = cell;
            this.receptor = receptor;

            if (!this.cell.receptorTypes.Contains(this)) this.cell.receptorTypes.Add(this);
        }

        public override void RemoveElement(ILinkable element)
        {
            if (element is Receptor)
            {
                Receptor r = (Receptor)element;
                if (this.receptor == r)
                {
                    this.receptor = null;
                    Model.Current.RemoveElement(this);
                }
            }
            else if (element is CellType)
            {
                CellType ct = (CellType)element;
                if (this.cell == ct)
                {
                    this.cell = null;
                    Model.Current.RemoveElement(this);
                }
            }
        }

        [JsonIgnore]
        public override string Name
        {
            get
            {
                if (cell == null || receptor == null) return "Broken Cell-Receptor Link";
                return string.Format("{0}<->{1} interactions", cell.Name, receptor.Name);
            }
            set
            {
                return;
            }
        }
    }
}
