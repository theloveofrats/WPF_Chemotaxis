using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using WPF_Chemotaxis.Model;
using WPF_Chemotaxis.Simulations;
using WPF_Chemotaxis.UX;
using WPF_Chemotaxis.VisualScripting;

namespace WPF_Chemotaxis.CorePlugin
{
    [VSElement(symbolResourcePath = "Core_Plugin;component/Resources/DirectionModuleIcon.png", symbolSize = 7.0, ui_TypeLabel = "Cell Direction Logic", tagX = 25, tagY = 0, tagCentre = false)]
    public class CellLogic_PBRW: LabelledLinkable, ICellComponent
    {
        private string name = "Persistent Biased Walker";                                           // This is the field where the display name is stored
        public override string Name {                                                               // This is the accessor of the above field 'name'. This is the field 
            get {                                                                                   // that the program actually interacts with (which is why it is public, not private)
                return name;
            }
            set
            {
                name = value;
            }
        }

        [JsonIgnore]                                                                                // [JsonIgnore] is a tag you need to put on fields that aren't saved as part of the model. 
        IDictionary<Cell, PBRWParams> paramRefs = new Dictionary<Cell, PBRWParams>();               // This field is used in active simulations, not in model definition, so it isn't saved.
           
        public CellLogic_PBRW() : base()
        {
            Init();
        }                                                                                           // Because each cell can have a parameter value on a spectrum of values, this dictionary allows us to
                                                                                                    // pass in a cell and look up the table of values used by that cell. 
        public override string DisplayType => "Cell movement logic";                                // The "Type" field in the display window where you set up a model. Like Cell or Receptor.

        [Param(Name = "Persistence", Min = 0, Max = 1)]                                             // Parameters that are shown in the parameters box have details entered in a tag above them.
        public CenteredDoubleRange persistence { get; set; } = new CenteredDoubleRange(0.75,0);     // so in this example, we have given it the on-screen name 'Persistence' and entered a min and max
                                                                                                    // which will stop people from putting in values lower than min or higher than max. A CentredDoubleRange(A,B)
        [Param(Name = "Chemokinesis Factor", Min = 0)]                                              // is a custom type that will allow the parameter to average A whilst varying by B across a population.
        public CenteredDoubleRange chemokinesis { get; set; } = new CenteredDoubleRange(0,0);

        [Param(Name = "Chemotaxis Strength", Min=0)]
        public CenteredDoubleRange chemotaxis_power { get; set; } = new CenteredDoubleRange(20,0);

       
        // OKAY, I NEED A GAUSSIAN GENERATOR< LOOK UP MY PERSONAL RANDOM NUMBER GENERATOR!

        public virtual void Initialise(Simulation sim)                                              // Initialise is called once at the begining of a running simulation and lets you set up any logic you. 
        {                                                                                           // need to before anything happens. I this case, I use it to:
            paramRefs.Clear();                                                                      //      A) react to user changes of parameter values (here, run the local function UpdateParams()) 
            this.PropertyChanged += (s, e) => UpdateParams(this, sim);                              //      B) put all cells in the register for looking up individual parameter values during the run.
            sim.CellAdded += this.RegisterCell;
        }

        public void ConnectToCellType(CellType ct)
        {
            new ExpressionCoupler(this, ct);
        }
        

        private void RegisterCell(Simulation sim, CellNotificationEventArgs e)                                        // This function just puts cells in the look-up list if they're not already there.  
        {
            if (e.NewCell.CellType.components.Contains(this))
            {
                lock (paramRefs)                                                                        // The list is lockedwhile this happens to stop simultaneous access by different threads (because it's generally
                {                                                                                       // bad to change the numbers you're calculating with halfway through a calculation!)
                    if (!paramRefs.ContainsKey(e.NewCell))
                    {
                        PBRWParams par = new PBRWParams(e.NewCell.Id);
                        par.Update(this, sim);
                        paramRefs.Add(e.NewCell, par);
                    }
                }
            }
        }

        private void UpdateParams(CellLogic_PBRW logic, Simulation sim)                             // This was linked in Initialse. If a user changes parameter values halfway through a sim, this updates all cells
        {                                                                                           // with these new parameter values. 
            foreach(Cell key in paramRefs.Keys)
            {
                paramRefs[key].Update(logic, sim);
            }
        }

        // A bit rough
        public virtual void Update(Cell cell, Simulations.Simulation sim, Simulations.Environment env)       // Update is called every time-step and is where the core logic goes. 
        {
            double mean_activity;

            PBRWParams checkrefs;
            if (paramRefs.TryGetValue(cell, out checkrefs))
            {

                Vector newDir = EnvironmentSnapshot(env, cell, checkrefs, out mean_activity);                                                 // EnvironmentSnapshot(env,cell,out mean_activity) returns a vector pointing in the
                                                                                                                                              // direction of receptor activity bias for the Cell cell in the Environment env.
                newDir += PersistenceDirection(cell, checkrefs);                                                                          // mean_activity (or whatever double you pass as an out variable in the third argument) 
                                                                                                                                          //System.Diagnostics.Debug.Print(string.Format("PB dir:: ({0:0.00},{1:0.00})", newDir.X, newDir.Y));               // now contains the mean activity value of all receptors, between 0 and 1
                                                                                                                                          // We also add the persistence direction here.
                double chemokinesis = checkrefs == null ? 0 : checkrefs.chemokinesis;

                newDir.Normalize();                                                                                                // We then normalise...
                newDir *= (1d + chemokinesis * mean_activity) * cell.Speed;                                        // then multiply by cell speed to keep movement rate as expected.
                cell.UpdateIntendedMovementDirection(newDir.X, newDir.Y);
            }                                                                                                   // We then tell the main simulation where this cell wants to go. We do not put this cell there,
        }                                                                                                                      // because the main simulation might know about obstacles, other cells and flow that could
                                                                                                                               // make moving there impossible. 
        private Vector PersistenceDirection(Cell cell, PBRWParams refs)                                                                                  // Time independent persistence calculation. This means that persistence behavour
        {                                                                                                                               // remains constant under changes to simulation delta-t. If you want persistence, best
            double previousTheta = 0;      // use this as-is to find appropriate directional choices

            if (cell.vx==0 && cell.vy==0) previousTheta = refs.Rnd.NextDouble(-Math.PI, Math.PI); 
            else previousTheta = Math.Atan2(cell.vy, cell.vx);
            double theta = refs.Rnd.NextGaussian(previousTheta, refs.sigma);
            //System.Diagnostics.Debug.Print(string.Format("theta_a:: {0:0.00})", theta));
            theta = theta%(2*Math.PI);

            return new Vector(Math.Cos(theta), Math.Sin(theta));
        }

        protected virtual Vector EnvironmentSnapshot(Simulations.Environment environment, Cell cell, PBRWParams refs, out double mean_eff_total)         // This is where the cell tots up the influence of all receptors and finds the current
        {                                                                                                                               // bias direction. 

            double moment_x, moment_y;


            CellType ct = cell.CellType;                                                                                                // We get a reference to the model CellType
            double pwr = refs.chemotactic_strength;                                                                          // and find how chemotactic our current cell instance is 
            mean_eff_total = moment_x = moment_y = 0;

            //System.Diagnostics.Debug.Print(string.Format("Doing snapshot with x,y :: ({0},{1})", moment_x,moment_y));

            Receptor r;
            Vector dir;
            if (ct.receptorTypes.Count() > 0)                                                                                             // Then, if the cell type has at least one receptor type
            {
                foreach (ExpressionCoupler crr in ct.receptorTypes)                                                                  // across all receptor types
                {
                    r = (Receptor) crr.ChildComponent;
                    if (r == null) continue;

                    mean_eff_total += cell.ReceptorActivity(r);                                                                         

                    dir = cell.ReceptorDifference(r);                                                                                   // We ask the cell to report its receptor occupancy difference
                 
                    moment_x += dir.X * cell.ReceptorWeight(r);                                                                         // and we multiply the importance of this by the receptor class's weight for this individual  cell
                    moment_y += dir.Y * cell.ReceptorWeight(r);                                                                         // individual because these are ranged values too, and can differ cell to cell)
                }
                mean_eff_total /= ct.receptorTypes.Count();
                moment_x *= pwr / ct.receptorTypes.Count();                                                                               // Finally, we multiply this total receptor difference by our chemotaxis power...
                moment_y *= pwr / ct.receptorTypes.Count();                                                                                           

                //System.Diagnostics.Debug.Print(string.Format("moment:: ({0:0.00},{1:0.00})", moment_x, moment_y));
            }

            return new Vector(moment_x, moment_y);                                                                                      // ... and send it back to the requester.
        }
    }
}
