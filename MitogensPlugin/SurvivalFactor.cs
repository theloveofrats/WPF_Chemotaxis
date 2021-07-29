using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Chemotaxis.Simulations;
using WPF_Chemotaxis;
using WPF_Chemotaxis.UX;
using WPF_Chemotaxis.Model;
using Newtonsoft.Json;

namespace MitogensPlugin
{
    class SurvivalFactor
    {
        private Dictionary<Cell, double> buffer = new(); // Record of how long since each cell last split. 
        private SciRand rnd = new();

        [JsonProperty]      // This makes the dropdown selection saveable.
        [InstanceChooser(label = "Input receptor")] // This creates a dropdown of all instances of the type
                                                    // (Receptor here), so you can choose the one to plug in.
        public Receptor Input;

        [Param(Name = "Threshold Occupancy", Min = 0, Max = 1)]
        public double threshold { get; set; } = 0.1;

        [Param(Name = "Death above threshold")]
        public bool inverse { get; set; } = false;

        [Param(Name = "Min Delay", Min = 0)]
        public double delay { get; set; } = 0;

        [Param(Name = "Death rate", Min = 0, Max = 1)]
        public double rate { get; set; } = 0.05;

        private Simulation.CellNotificationHandler RegisterCell; // We don't know how many components cells will have,
                                                          // or how many cells a simulation will have, so we
                                                          // deal with their initialisation using events and
                                                          // then components subscribe to any they are interested
                                                          // in. This is a handler for a new cell being added.

        public void Initialise(Simulation sim) //Initialise is a required part of a cell component.
                                               //Called once when the simulation starts.
        {
            RegisterCell = (sim, cell, args) => RegisterNewCell(sim, cell); // We have used it to subscribe to the
                                                                      // "Cell Added" event in the simulation.
            sim.CellAdded += RegisterCell;
        }

        // Update called every dt!
        public void Update(Cell cell, Simulation sim, WPF_Chemotaxis.Simulations.Environment env, IFluidModel flow)
        {
            if (cell.ReceptorActivity(this.Input) > threshold && inverse
               || cell.ReceptorActivity(this.Input) < threshold && !inverse){

                if (buffer[cell] < delay)
                {
                    buffer[cell] += sim.Settings.dt;
                    return;
                }
                else
                {
                    lock (rnd)
                    {
                        if (rnd.value < rate * sim.Settings.dt)  // We randomly, with an average rate
                                                                 // specified by the rate parameter
                        {
                            // Remove cell from simulation!
                            sim.RemoveCell(cell, CellDeathType.APOPTOTIC);
                        }
                    }
                }
            }
            else //Reset buffer.
            {
                buffer[cell] = 0;
            }
        }

        private void RegisterNewCell(Simulation sim, Cell cell)
        {
            if (!buffer.ContainsKey(cell)) buffer.Add(cell, 0);
        }
    }
}
