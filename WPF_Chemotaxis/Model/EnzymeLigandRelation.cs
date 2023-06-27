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
    public class EnzymeLigandRelation : LabelledLinkable
    {
        [JsonProperty]
        [Link]
        public CellSurfaceEnzyme Enzyme { get; private set; }

        [JsonProperty]
        [InstanceChooser(label = "Input ligand")]
        [VisualLine(parentAnchor = LineAnchorType.ANCHOR_FORWARD, childAnchor = LineAnchorType.ANCHOR_CENTRE, parentAnchorDistance = 25.0, childAnchorDistance = 12.0, parentArrowHead = LineHeadType.ARROW)]

        public Ligand Ligand { get; private set; }

        [JsonProperty]
        [VisualLine(parentAnchor = LineAnchorType.ANCHOR_FORWARD, childAnchor = LineAnchorType.ANCHOR_CENTRE, parentAnchorDistance = 25.0, childAnchorDistance = 18.0, childArrowHead = LineHeadType.ARROW)]
        [InstanceChooser(label = "Product ligand")]
        public Ligand ProductLigand{ get; private set; }


        //DEBUG REMOVE!
        Random random = new Random();

        [Param(Name = "Input:Output")]
        public double multiplier { get; set; } = 1;
        [Param(Name = "vMax")]
        public double vMax { get; set; } = 10;
        [Param(Name = "Hill Coeficient")]
        public double Hill { get; set; } = 1;
        [Param(Name = "kM")]
        public double kM { get; set; } = 0.5;

        public EnzymeLigandRelation() : base() 
        {
            Init();
        }

        public EnzymeLigandRelation(CellSurfaceEnzyme enzyme, Ligand ligand) : base()
        {
            this.Enzyme = enzyme;
            this.Ligand = ligand;
            Init();
        }

        public void SetLigand(Ligand newLigand)
        {
            this.Ligand = newLigand;
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
                if (this.Ligand == l)
                {
                    this.Ligand = (Ligand)replacement;
                }
                if (this.ProductLigand == l)
                {
                    this.ProductLigand = (Ligand)replacement;
                }
                if (this.Ligand == null)
                {
                    Model.Current.RemoveElement(this);
                }
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
