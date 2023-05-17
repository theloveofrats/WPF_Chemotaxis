using System;
using System.Collections.Generic;
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
    public class CellSurfaceEnzyme : LabelledLinkable, ICellComponent
    {
        public string label = "Receptor";

        [LinkAttribute]
        public List<EnzymeLigandRelation> ligandInteractions = new();

        public CellSurfaceEnzyme() : base()
        {
            Init();
        }

        public CellSurfaceEnzyme(string label) : base(label)
        {
            Init();
        }

        [ElementAdder(label = "Add Ligand", type = typeof(Ligand))]
        public void AddLigand(Ligand ligand)
        {
            foreach (var inter in ligandInteractions)
            {
                if (inter.Ligand.Equals(ligand)) return;
            }
            new EnzymeLigandRelation(this, ligand);
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
                foreach (EnzymeLigandRelation elr in this.ligandInteractions)
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
                        this.ligandInteractions.Remove(found);
                    }
                }
                else
                {
                    foreach (EnzymeLigandRelation elr in this.ligandInteractions)
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
                if (this.ligandInteractions.Contains(elr))
                {
                    this.ligandInteractions.Remove(elr);
                    if (replacement != null && replacement.GetType().IsAssignableTo(typeof(EnzymeLigandRelation)))
                    {
                        this.ligandInteractions.Add((EnzymeLigandRelation)replacement);
                    }
                }
            }
        }

        public void Update(Cell cell, Simulation sim, Simulations.Environment env, IFluidModel flow)
        {
            foreach (Point p in cell.localPoints)
            {



                double rate = vMax / c.localPoints.Count;

                //Offload to a proper solver later!
                if (input_ligand != null)
                {
                    double in_cnc = env.GetConcentration(input_ligand, p.X, p.Y);



                    if (Hill == 1)
                    {
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
        }

        private void GetOccupancy(Simulations.Environment environment, int x, int y)
        {
            double ckd_sum = 0;
            foreach (LigandReceptorRelation lrr in this.ligandInteractions)
            {
                ckd_sum += environment.GetConcentration(lrr.Ligand, x, y) / lrr.kD;
            }

            return ckd_sum / (ckd_sum + 1.0);
        }
        public double GetEfficacy(Simulations.Environment environment, double x, double y)
        {
            return GetEfficacy(environment, (int)(x / environment.settings.DX), (int)(y / environment.settings.DX));
        }

        public double GetEfficacy(Simulations.Environment environment, int x, int y)
        {
            double ckd_top = 0;
            double ckd_btm = 0;

            double affinityPart;
            foreach (LigandReceptorRelation lrr in this.ligandInteractions)
            {
                affinityPart = environment.GetConcentration(lrr.Ligand, x, y) / lrr.kD;
                ckd_btm += affinityPart;
                ckd_top += affinityPart * lrr.eff;
            }

            return ckd_top / (ckd_btm + 1.0);
        }
    }
}
