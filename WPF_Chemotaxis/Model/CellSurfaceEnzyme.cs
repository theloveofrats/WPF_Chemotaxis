using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Chemotaxis.Simulations;
using WPF_Chemotaxis.UX;
using WPF_Chemotaxis.VisualScripting;
using System.Windows.Media.Media3D;
using System.Runtime.CompilerServices;

namespace WPF_Chemotaxis.Model
{
    /// <summary>
    /// Representation of a receptor type in the theoretical model. Links to some number of ligands. During a simulation, 
    /// each receptor will take input ligand concentations from a cell and respond with levels of occupancy and activity.
    /// </summary>
    [VSElementAttribute(ui_TypeLabel = "Surface Enzyme", symbolResourcePath = "Resources/EnzymeIcon.png", symbolSize = 6.0, tagX = 12, tagY = -12, tagCentre = false)]
    public class CellSurfaceEnzyme : LabelledLinkable, ICellComponent
    {
        public string label = "Enzyme";

        [Link]
        [VisualLine(parentAnchor = LineAnchorType.ANCHOR_FORWARD, childAnchor = LineAnchorType.ANCHOR_CENTRE, parentAnchorDistance = 5.0, childAnchorDistance = 5.0)]
        public List<EnzymeLigandRelation> substrateInteractions { get; private set; } = new();
        public CellSurfaceEnzyme() : base()
        {
            Init();
        }

        public CellSurfaceEnzyme(string label) : base(label)
        {
            Init();
        }

        [ElementAdder(label = "Add Substrate", type = typeof(Ligand))]
        public void AddLigand(Ligand ligand)
        {
            foreach (var inter in substrateInteractions)
            {
                if (inter.Ligand==ligand) return;
            }
            this.substrateInteractions.Add(new EnzymeLigandRelation(this, ligand));
        }

        private Dictionary<Cell, double> expressionWeights;
        protected void RegisterCell(Cell newCell)
        {
            CenteredDoubleRange weightRange;
            if (newCell.CellType.TryGetWeight(this, out weightRange)){
                lock (expressionWeights)
                {
                    double val = weightRange.RandomInRange;
                    expressionWeights.Add(newCell, val);
                }
            }
        }

        public void Update(Cell simCell, Simulation sim, Simulations.Environment env)
        {
            double val;
            if (expressionWeights.TryGetValue(simCell, out val))
            {
                this.UpdateCell(simCell, sim, env, val);
            }
            else
            {
                RegisterCell(simCell);
                this.UpdateCell(simCell, sim, env, expressionWeights[simCell]);
            }
        }
        public void UpdateCell(Cell cell, Simulation sim, Simulations.Environment env, double weight)
        {

            foreach (Point p in cell.localPoints)
            {
                foreach (EnzymeLigandRelation elr in this.substrateInteractions)
                {
                    double rate = weight * elr.vMax / cell.localPoints.Count;
                    rate *= GetOccupancyFraction(elr, env, p.X, p.Y);

                    //Offload to a proper solver later!
                    env.DegradeAtRate(elr.Ligand, elr.ProductLigand, p.X, p.Y, rate, elr.multiplier, sim.Settings.dt);
                }
            }
        }

        public void Initialise(Simulation sim)
        {
            this.expressionWeights = new();
        }

        public void ConnectToCellType(CellType ct)
        {
            ct.TryAddExpression(this);
        }

        public override void RemoveElement(ILinkable element, ILinkable replacement = null)
        {
            if (element is Ligand)
            {
                Ligand lig = (Ligand)element;
                EnzymeLigandRelation found = null;
                foreach (EnzymeLigandRelation elr in this.substrateInteractions)
                {
                    if (elr.Ligand == lig)
                    {
                        found = elr;
                        break;
                    }
                }
                if (found != null)
                {
                    if (replacement != null && replacement.GetType().IsAssignableTo(typeof(Ligand)))
                    {
                        found.SetLigand((Ligand)replacement);
                    }
                    else
                    {
                        this.substrateInteractions.Remove(found);
                    }
                }
                else
                {
                    foreach (EnzymeLigandRelation elr in this.substrateInteractions)
                    {
                        if (elr.ProductLigand == lig)
                        {
                            found = elr;
                            break;
                        }
                    }
                    if (found != null)
                    {
                        if (replacement != null && replacement.GetType().IsAssignableTo(typeof(Ligand)))
                        {
                            found.SetProduct((Ligand)replacement);
                        }
                        else
                        {
                            found.SetProduct(null);
                        }
                    }
                }
            }
            else if (element is EnzymeLigandRelation)
            {
                EnzymeLigandRelation elr = (EnzymeLigandRelation)element;
                if (this.substrateInteractions.Contains(elr))
                {
                    this.substrateInteractions.Remove(elr);
                    if (replacement != null && replacement.GetType().IsAssignableTo(typeof(EnzymeLigandRelation)))
                    {
                        this.substrateInteractions.Add((EnzymeLigandRelation)replacement);
                    }
                }
            }
        }



        
        private double GetOccupancyFraction(EnzymeLigandRelation ligandRelation, Simulations.Environment environment, double x, double y)
        {
            return GetOccupancyFraction(ligandRelation, environment, (int)(x / environment.settings.DX), (int)(y / environment.settings.DX));
        }
        private double GetOccupancyFraction(EnzymeLigandRelation ligandRelation, Simulations.Environment environment, int x, int y)
        {
            if (!this.substrateInteractions.Contains(ligandRelation)) return 0;

            double btm = 0;
            double top = environment.GetConcentration(ligandRelation.Ligand, x, y) / ligandRelation.kM;

            foreach (EnzymeLigandRelation elr in this.substrateInteractions)
            {
                btm += environment.GetConcentration(elr.Ligand, x, y) / elr.kM;
            }

            return top / (btm + 1.0);
        }

        // SHOULD BE REMOVED AS ADDER ALREADY WORKS AT CELL END
        public override bool TryAddTo(ILinkable link)
        {
            if (link is CellType)
            {
                var cellType = link as CellType;
                cellType.AddCellLogicComponent(this);
                return true;
            }
            else return base.TryAddTo(link);
        }
    }
}
