using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Chemotaxis.UX;
using WPF_Chemotaxis.Simulations;
using System.Windows;
using Newtonsoft.Json;
using WPF_Chemotaxis.VisualScripting;

namespace WPF_Chemotaxis.Model
{
    /// <summary>
    /// Relational class for cell-ligand direct interactions (rather than receptor-mediated interactions).
    /// </summary>

    [LineConnector(parentPropertyName = "Enzyme")]
    public class EnzymeLigandRelation : LigandRelation
    {
        [JsonProperty]
        [Link]
        public CellSurfaceEnzyme Enzyme { get; private set; }

        [JsonProperty]
        [VisualLine(parentAnchor = LineAnchorType.ANCHOR_FORWARD, childAnchor = LineAnchorType.ANCHOR_CENTRE, parentAnchorDistance = 25.0, childAnchorDistance = 18.0, childArrowHead = LineHeadType.ARROW)]
        [InstanceChooser(label = "Product ligand")]
        public Ligand ProductLigand{ get; private set; }


        //DEBUG REMOVE!
        Random random = new Random();

        [Param(Name = "Input:Output")]
        public double multiplier { get; set; } = 1;
        [Param(Name = "vMax")]
        public double vMax { get; set; } = 40;
        [Param(Name = "Hill Coeficient")]
        public double Hill { get; set; } = 1;

        public EnzymeLigandRelation() : base() 
        {
            Init();
        }

        public EnzymeLigandRelation(CellSurfaceEnzyme enzyme, Ligand ligand) : base(ligand)
        {
            this.Enzyme = enzyme;
            Init();
        }

        public void SetProduct(Ligand newLigand)
        {
            this.ProductLigand = newLigand;
        }

        public override void RemoveElement(ILinkable element, ILinkable replacement = null)
        {
            if (element is Ligand)
            {
                Ligand l = (Ligand)element;
               
                if (this.ProductLigand == l)
                {
                    this.ProductLigand = (Ligand)replacement;
                }
                if (this.Ligand == null)
                {
                    Model.Current.RemoveElement(this);
                }
                else base.RemoveElement(element, replacement);
            }
            else if (element is CellSurfaceEnzyme)
            {
                CellSurfaceEnzyme enz = (CellSurfaceEnzyme)element;
                if (this.Enzyme == enz)
                {
                    this.Enzyme = (CellSurfaceEnzyme)replacement;

                    if (this.Enzyme == null) Model.Current.RemoveElement(this);
                }
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
                else if (Enzyme == null || Ligand == null) return "Broken Enzyme-Ligand interaction";
                else if (ProductLigand == null) return string.Format("New {0}-{1} interaction", Enzyme.Name, Ligand.Name);
                else return string.Format("New {0}->{1}->{2} reaction", Ligand.Name, Enzyme.Name, ProductLigand.Name);
            }
            set
            {
                name = value;
            }
        }
    }
}
