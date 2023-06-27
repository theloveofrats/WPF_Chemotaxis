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
    [Dockable(parentPropertyName = "Cell", childPropertyName = "Enzyme")]
    public class CellEnzymeRelation : LabelledLinkable, ICellComponent
    {
        [JsonProperty]
        [Link]
        public CellSurfaceEnzyme Enzyme { get; private set; }
        [JsonProperty]
        [Link]
        public CellType Cell { get; private set; }

        [Param(Name = "Enzyme expression weight", Min = 0)]
        public CenteredDoubleRange Weight { get; set; } = new CenteredDoubleRange(1, 0);

        public CellEnzymeRelation() : base()
        {
            Init();
        }
        public CellEnzymeRelation(string label) : base(label) 
        {
            Init();
        }

        public CellEnzymeRelation(CellType cell, CellSurfaceEnzyme enzyme) : base()
        {
            this.Cell = cell;
            this.Enzyme = enzyme;
            Init();
        }

        public override void RemoveElement(ILinkable element, ILinkable replacement = null)
        {
            if (element is CellSurfaceEnzyme)
            {
                CellSurfaceEnzyme enz = (CellSurfaceEnzyme)element;
                if (this.Enzyme == enz)
                {
                    this.Enzyme = (CellSurfaceEnzyme)replacement;
                    if (this.Enzyme == null) Model.Current.RemoveElement(this);
                }
            }
            else if (element is CellType)
            {
                CellType ct = (CellType)element;
                if (this.Cell == ct)
                {
                    this.Cell = (CellType)replacement;
                    if (this.Cell == null) Model.Current.RemoveElement(this);
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
                this.Enzyme.Update(simCell, sim, env, val);
            }
            else
            {
                RegisterCell(simCell);
                this.Enzyme.Update(simCell, sim, env, expressionWeights[simCell]);
            }
        }

        public void Initialise(Simulation sim)
        {
            this.expressionWeights = new();
        }

        public void ConnectToCellType(CellType ct)
        {
           
        }

        [JsonIgnore]
        public override string Name
        {
            get
            {
                if (Cell == null || Enzyme == null) return "Broken Cell-Enzyme Link";
                return string.Format("{0} expression of {1}", Cell.Name, Enzyme.Name);
            }
            set
            {
                return;
            }
        }
    }
}
