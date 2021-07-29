using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WPF_Chemotaxis.Model;
using WPF_Chemotaxis.Simulations;
using WPF_Chemotaxis.UX;

namespace WPF_Chemotaxis.CorePlugin
{
    public class CellLogic_PBRW: LabelledLinkable, ICellComponent
    {
        private string name = "Persistent Biased Walker";
        public override string Name {
            get {
                return name;
            }
            set
            {
                name = value;
            }
        }

        [JsonIgnore]
        IDictionary<Cell, PBRWParams> paramRefs = new Dictionary<Cell, PBRWParams>();

        public override string DisplayType => "Cell movement logic";

        [Param(Name = "Persistence", Min = 0, Max = 1)]
        public CenteredDoubleRange persistence { get; set; } = new CenteredDoubleRange(0.75,0);

        [Param(Name = "Chemokinesis Factor", Min = 0)]
        public CenteredDoubleRange chemokinesis { get; set; } = new CenteredDoubleRange(0,0);

        [Param(Name = "Chemotaxis Strength", Min=0)]
        public CenteredDoubleRange chemotaxis_power { get; set; } = new CenteredDoubleRange(20,0);

       
        // OKAY, I NEED A GAUSSIAN GENERATOR< LOOK UP MY PERSONAL RANDOM NUMBER GENERATOR!

        public virtual void Initialise(Simulation sim)
        {
            paramRefs.Clear();
            this.PropertyChanged += (s, e) => UpdateParams(this, sim);
            foreach(Cell cell in sim.Cells)
            {
                RegisterCell(sim, cell);
            }
            sim.CellAdded += (s, c, args) => this.RegisterCell(s, c);
        }

        private void RegisterCell(Simulation sim, Cell cell)
        {
            lock (paramRefs)
            {
                if (!paramRefs.ContainsKey(cell))
                {
                    PBRWParams par = new PBRWParams(cell.Id);
                    par.Update(this, sim);
                    paramRefs.Add(cell, par);
                }
            }
        }

        private void UpdateParams(CellLogic_PBRW logic, Simulation sim)
        {
            foreach(Cell key in paramRefs.Keys)
            {
                paramRefs[key].Update(logic, sim);
            }
        }

        // A bit rough
        public virtual void Update(Cell cell, Simulations.Simulation sim, Simulations.Environment env, IFluidModel flow)
        {
            double mean_activity;
            Vector newDir = EnvironmentSnapshot(env, cell, out mean_activity);
            //System.Diagnostics.Debug.Print(string.Format("Bias:: ({0:0.00},{1:0.00})", newDir.X, newDir.Y));
                newDir += PersistenceDirection(cell);
            //System.Diagnostics.Debug.Print(string.Format("PB dir:: ({0:0.00},{1:0.00})", newDir.X, newDir.Y));


            newDir.Normalize();
            newDir *= (1d + paramRefs[cell].chemokinesis * mean_activity) * cell.Speed;
               cell.UpdateIntendedMovementDirection(newDir.X, newDir.Y);
        }

        private Vector PersistenceDirection(Cell cell)
        {
            double previousTheta = 0;
            if (cell.vx==0 && cell.vy==0) previousTheta = paramRefs[cell].Rnd.NextDouble(-Math.PI, Math.PI); 
            else previousTheta = Math.Atan2(cell.vy, cell.vx);
            double theta = Math.Clamp(paramRefs[cell].Rnd.NextGaussian(previousTheta, paramRefs[cell].sigma), -Math.PI, Math.PI);

            return new Vector(Math.Cos(theta), Math.Sin(theta));
        }

        protected virtual Vector EnvironmentSnapshot(Simulations.Environment environment, Cell cell, out double mean_eff_total)
        {

            double moment_x, moment_y;


            CellType ct = cell.CellType;
            double pwr = paramRefs[cell].chemotactic_strength;
            mean_eff_total = moment_x = moment_y = 0;

            //System.Diagnostics.Debug.Print(string.Format("Doing snapshot with x,y :: ({0},{1})", moment_x,moment_y));

            Receptor r;
            Vector dir;
            if (ct.receptorTypes.Count > 0)
            {
                foreach (CellReceptorRelation crr in ct.receptorTypes)
                {
                    r = crr.Receptor;

                    mean_eff_total += cell.ReceptorActivity(r);

                    dir = cell.ReceptorDifference(r);
                    //System.Diagnostics.Debug.Print(string.Format("dir:: ({0:0.00},{1:0.00})      weight:: {2:0.00}",dir.X, dir.Y, cell.ReceptorWeight(r)));
                    moment_x += dir.X * cell.ReceptorWeight(r);
                    moment_y += dir.Y * cell.ReceptorWeight(r);
                }
                mean_eff_total /= ct.receptorTypes.Count;
                moment_x *= pwr / ct.receptorTypes.Count;
                moment_y *= pwr / ct.receptorTypes.Count;

                //System.Diagnostics.Debug.Print(string.Format("moment:: ({0:0.00},{1:0.00})", moment_x, moment_y));
            }

            return new Vector(moment_x, moment_y);
        }
    }
}
