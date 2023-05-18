using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Chemotaxis.Simulations;
using WPF_Chemotaxis.UX;
using WPF_Chemotaxis.VisualScripting;

namespace WPF_Chemotaxis.Model
{
    /// <summary>
    /// Representation of a receptor type in the theoretical model. Links to some number of ligands. During a simulation, 
    /// each receptor will take input ligand concentations from a cell and respond with levels of occupancy and activity.
    /// </summary>
    [VSElementAttribute(ui_TypeLabel = "Surface Enzyme", symbolResourcePath = "Resources/EnzymeIcon.png", symbolSize = 6.0, tagX = 12, tagY = -12, tagCentre = false)]
    public class CellSurfaceEnzyme : LabelledLinkable
    {
        public string label = "Receptor";

        [LinkAttribute]
        public List<EnzymeLigandRelation> substrateInteractions = new();

        public CellSurfaceEnzyme() : base()
        {
            Init();
        }

        public CellSurfaceEnzyme(string label) : base(label)
        {
            Init();
        }

        [ElementAdder(label = "Add Substrate", type = typeof(Ligand))]
        public void AddSigand(Ligand ligand)
        {
            System.Diagnostics.Debug.Print(String.Format("Invoked {0} to AddSubstrate {1}", this.Name, ligand.Name));
            foreach (var inter in substrateInteractions)
            {
                if (inter.Ligand.Equals(ligand)) return;
            }
            System.Diagnostics.Debug.Print(String.Format("Not curent substrate, so creating new enzyme relation..."));
            new EnzymeLigandRelation(this, ligand);
            System.Diagnostics.Debug.Print(String.Format("Created ENZYME-LIGAND LINK"));
        }

        public void Initialise(Simulation sim)
        {
            
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

        public void Update(Cell cell, Simulation sim, Simulations.Environment env, IFluidModel flow, double weight)
        {
            foreach (Point p in cell.localPoints)
            {
                foreach (EnzymeLigandRelation elr in this.substrateInteractions)
                {
                    double rate = weight*elr.vMax / cell.localPoints.Count;
                    GetOccupancyFraction(elr, env, p.X, p.Y);
                    //Offload to a proper solver later!
                    env.DegradeAtRate(elr.Ligand, elr.ProductLigand, p.X, p.Y, rate, elr.multiplier, sim.Settings.dt);
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

        public override bool TryAddTo(ILinkable link)
        {
            System.Diagnostics.Debug.Print(String.Format("Trying to add {0} to {1} via {0}'s TryAddTo", this.Name, link.Name));
            if (link is CellType)
            {
                var cellType = link as CellType;
                System.Diagnostics.Debug.Print(String.Format("Adding to cell", this.Name, link.Name));
                CellEnzymeRelation cer = new CellEnzymeRelation(cellType, this);
                System.Diagnostics.Debug.Print(String.Format("Created cell-enzyme relation!", this.Name, link.Name));
                cellType.AddCellLogicComponent(cer);
                return true;
            }
            else return base.TryAddTo(link);
        }
    }
}
