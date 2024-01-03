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
                    if(!expressionWeights.ContainsKey(newCell)) expressionWeights.Add(newCell, val);
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
            /*
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
            */

            foreach (Point p in cell.localPoints)
            {
                foreach (EnzymeLigandRelation elr in this.substrateInteractions)
                {
                    double rate = weight * elr.vMax / cell.localPoints.Count;

                    double offsetX = cell.X % sim.Environment.settings.DX;
                    double offsetY = cell.Y % sim.Environment.settings.DX;

                    double c = env.GetConcentration(elr.Ligand, p.X+offsetX, p.Y+offsetY);

                    double f0 = rate * GetOccupancyFraction(elr, env, p.X+offsetX, p.Y+offsetY, out var cbykd_sum); //Zeroth order
                    double f1 = rate * (1.0 / elr.kD) * (1 + cbykd_sum - (c/elr.kD)) / ((1.0 + cbykd_sum) * (1.0 + cbykd_sum)); //First order

                    //Estimate rates after dt.
                    double c_est = Math.Max(c - rate*(f0 + f1 * c) * sim.Settings.dt, 0);
                    double f0_p1 = rate * GetOccupancyFraction(elr, env, p.X+offsetX, p.Y+offsetY, out var cbykd_2, c_est);
                    double f1_p1 = rate * (1.0 / elr.kD) * (1 + cbykd_sum - (c_est / elr.kD)) / ((1.0 + cbykd_sum) * (1.0 + cbykd_sum));

                    //Go with average rate of beginning and "end"
                    f0 = 0.5 * (f0 + f0_p1);
                    f1 = 0.5 * (f1 + f1_p1);

                    //Offload to a proper solver later!
                    env.DegradeAtRate(elr.Ligand, elr.ProductLigand, p.X, p.Y, f0-f1*c, f1, elr.multiplier, sim.Settings.dt);
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



        
        private double GetOccupancyFraction(EnzymeLigandRelation ligandRelation, Simulations.Environment environment, double x, double y, out double ckd_sum,double c_forced = -1)
        {
            return GetOccupancyFraction(ligandRelation, environment, (int)(x / environment.settings.DX), (int)(y / environment.settings.DX), out ckd_sum, c_forced);
        }
        private double GetOccupancyFraction(EnzymeLigandRelation ligandRelation, Simulations.Environment environment, int x, int y,  out double ckd_sum, double c_forced = -1)
        {
            if(c_forced == -1)
            {
                c_forced = environment.GetConcentration(ligandRelation.Ligand, x, y);
            }

            if (!this.substrateInteractions.Contains(ligandRelation))
            {
                ckd_sum = 0;
                return 0;
            }
            double btm = 0;
            double top = c_forced / ligandRelation.kD;

            foreach (EnzymeLigandRelation elr in this.substrateInteractions)
            {
                if (elr == ligandRelation)
                {
                    btm += c_forced / elr.kD;
                }
                else
                {
                    btm += environment.GetConcentration(elr.Ligand, x, y) / elr.kD;
                }
            }
            ckd_sum = btm;

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
