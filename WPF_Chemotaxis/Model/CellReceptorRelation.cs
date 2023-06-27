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
    /// Class quantifying the relationship between a cell type and a receptor type.
    /// </summary>
    [Dockable(parentPropertyName = "Cell", childPropertyName = "Receptor")]
    public class CellReceptorRelation : LabelledLinkable
    {
        [JsonProperty]
        [Link]
        public Receptor Receptor { get; private set; }
        [JsonProperty]
        [Link]
        public CellType Cell { get; private set; }

        [Param(Name = "Receptor weight", Min = 0)]
        public CenteredDoubleRange Weight { get; set; } = new CenteredDoubleRange(1,0);

        public CellReceptorRelation() : base() 
        {
            Init();
        }
        public CellReceptorRelation(string label) : base(label) { }

        public CellReceptorRelation(CellType cell, Receptor receptor) : base()
        {
            this.Cell = cell;
            this.Receptor = receptor;
            if (!this.Cell.receptorTypes.Contains(this)) this.Cell.receptorTypes.Add(this);
            Init();
        }

        public override void RemoveElement(ILinkable element, ILinkable replacement=null)
        {
            if (element is Receptor)
            {
                Receptor r = (Receptor)element;
                if (this.Receptor == r)
                {
                    this.Receptor = (Receptor) replacement;
                    if(this.Receptor ==null) Model.Current.RemoveElement(this);
                }
            }
            else if (element is CellType)
            {
                CellType ct = (CellType) element;
                if (this.Cell == ct)
                {
                    this.Cell = (CellType) replacement;
                    if(this.Cell==null) Model.Current.RemoveElement(this);
                }
            }
        }

        [JsonIgnore]
        public override string Name
        {
            get
            {
                if (Cell == null || Receptor == null) return "Broken Cell-Receptor Link";
                return string.Format("{0}<->{1} interactions", Cell.Name, Receptor.Name);
            }
            set
            {
                return;
            }
        }
    }
}
