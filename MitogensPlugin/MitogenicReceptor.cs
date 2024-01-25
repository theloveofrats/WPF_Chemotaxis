using System;
using WPF_Chemotaxis.Model;
using WPF_Chemotaxis.Simulations;
using WPF_Chemotaxis.UX;
using Newtonsoft.Json;
using System.Collections.Generic;
using WPF_Chemotaxis;

namespace WPF_Chemotaxis.MitogensPlugin
{
    public class MitogenicReceptor : LabelledLinkable, ICellComponent // Labelled linkable already does the heavy
                                                            // lifting to put this in the GUI. ICellComponent means
                                                            // it can be added to a cell to do extra logic.
    {
        private Dictionary<Cell, double> lastMitosis = new(); // Record of how long since each cell last split. 
        private SciRand rnd = new();

        [JsonProperty]      // This makes the dropdown selection saveable.
        [InstanceChooser(label = "Input receptor")] // This creates a dropdown of all instances of the type
        public Receptor Input;

        [Param(Name = "Threshold Occupancy", Min = 0, Max = 1)]
        public double threshold { get; set; } = 0.5;

        [Param(Name = "Mitosis below, not above, threshold")]
        public bool inverse { get; set; } = false;

        [Param(Name = "Min Delay", Min = 0)]
        public double delay { get; set; } = 5;

        [Param(Name = "Rate", Min = 0, Max = 1)]
        public double rate { get; set; } = 0.1;

        private Simulation.CellNotificationHandler RegisterCell; // We don't know how many components cells will have,
                                                          // or how many cells a simulation will have, so we
                                                          // deal with their initialisation using events and
                                                          // then components subscribe to any they are interested
                                                          // in. This is a handler for a new cell being added.

        public void Initialise(Simulation sim) //Initialise is a required part of a cell component.
                                               //Called once when the simulation starts.
        {
            sim.CellAdded += (sim, e) => RegisterNewCell(sim, e.NewCell); // We have used it to subscribe to the
                                                                          // "Cell Added" event in the simulation.
        }

        // Update called every dt!
        public void Update(Cell cell, Simulation sim, WPF_Chemotaxis.Simulations.Environment env)
        {
            if (lastMitosis.TryGetValue(cell, out var last))
            {
                if (last < delay)
                {
                    lastMitosis[cell] += sim.Settings.dt; //If we haven't given the cell time
                                                          //to recover from its last split, wait.
                    return;
                }
                if (cell.WeightedActiveReceptorFraction < threshold && inverse
                    || cell.WeightedActiveReceptorFraction > threshold && !inverse) // If we're over the threshold
                {
                    lock (rnd)
                    {
                        if (rnd.value < rate * sim.Settings.dt)  // We randomly, with an average rate
                                                                 // specified by the rate parameter
                        {
                            // Add a cell and set our time since mitosis to 0;
                            sim.AddCell(
                                cell.CellType, cell.X + 2.0 * cell.radius * (rnd.value - 0.5),
                                cell.Y + 2.0 * cell.radius * (rnd.value - 0.5),
                                CellEventType.MITOTIC
                            );
                            this.lastMitosis[cell] = 0;
                        }
                    }
                }
            }
        }

        private void RegisterNewCell(Simulation sim, Cell cell)
        {
            lock (lastMitosis)
            {
                if (cell.CellType.components.Contains(this))
                {
                    if (!lastMitosis.ContainsKey(cell)) lastMitosis.Add(cell, 0);

                }
            }
        }

        public void ConnectToCellType(CellType ct)
        {
            
        }
    }
}
