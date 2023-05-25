using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Chemotaxis.UX;
using Newtonsoft.Json;
using WPF_Chemotaxis.VisualScripting;
using WPF_Chemotaxis.Simulations;

namespace WPF_Chemotaxis.Model
{
    /// <summary>
    /// Class quantifying the relationship between a cell type and a receptor type.
    /// </summary>
    [VSRelationAttribute(forcedPositionType = ForcedPositionType.RADIUS, forcePositionDistance = 125, childFieldName = "enzyme", parentFieldName = "cell")]
    public class CellEnzymeRelation : LabelledLinkable, ICellComponent
    {
        [JsonProperty]
        [Link]
        private CellSurfaceEnzyme enzyme;
        public CellSurfaceEnzyme Enzyme
        {
            get
            {
                return enzyme;
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

        [Param(Name = "Enzyme expression weight", Min = 0)]
        public CenteredDoubleRange Weight { get; set; } = new CenteredDoubleRange(1, 0);

        public CellEnzymeRelation() : base()
        {
            Init();
        }
        public CellEnzymeRelation(string label) : base(label) { }

        public CellEnzymeRelation(CellType cell, CellSurfaceEnzyme enzyme) : base()
        {
            this.cell = cell;
            this.enzyme = enzyme;
            Init();
        }

        public override void RemoveElement(ILinkable element, ILinkable replacement = null)
        {
            if (element is CellSurfaceEnzyme)
            {
                CellSurfaceEnzyme enz = (CellSurfaceEnzyme)element;
                if (this.enzyme == enz)
                {
                    this.enzyme = (CellSurfaceEnzyme)replacement;
                    if (this.enzyme == null) Model.Current.RemoveElement(this);
                }
            }
            else if (element is CellType)
            {
                CellType ct = (CellType)element;
                if (this.cell == ct)
                {
                    this.cell = (CellType)replacement;
                    if (this.cell == null) Model.Current.RemoveElement(this);
                }
            }
        }

        private Dictionary<Cell, double> expressionWeights;

        protected void RegisterCell(Cell newCell)
        {
            lock (expressionWeights)
            {
                double val = Weight.RandomInRange;
                expressionWeights.Add(newCell, val);
            }
        }
        public void Update(Cell simCell, Simulation sim, Simulations.Environment env)
        {
            double val;
            if (expressionWeights.TryGetValue(simCell, out val))
            {
                this.enzyme.Update(simCell, sim, env, val);
            }
            else
            {
                RegisterCell(simCell);
                this.enzyme.Update(simCell, sim, env, expressionWeights[simCell]);
            }
        }

        public void Initialise(Simulation sim)
        {
            this.expressionWeights = new();
        }

        [JsonIgnore]
        public override string Name
        {
            get
            {
                if (cell == null || enzyme == null) return "Broken Cell-Enzyme Link";
                return string.Format("{0} expression of {1}", cell.Name, enzyme.Name);
            }
            set
            {
                return;
            }
        }
    }
}
