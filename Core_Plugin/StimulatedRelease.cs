using System;
using WPF_Chemotaxis.Model;
using WPF_Chemotaxis.Simulations;
using WPF_Chemotaxis.UX;
using Newtonsoft.Json;
using System.Collections.Generic;
using WPF_Chemotaxis;
using System.Windows;

namespace WPF_Chemotaxis.CorePlugin
{
    public class StimulatedRelease : LabelledLinkable, ICellComponent // Labelled linkable already does the heavy
                                                            // lifting to put this in the GUI. ICellComponent means
                                                            // it can be added to a cell to do extra logic.
    {
        private Dictionary<Cell, double> lastPulse = new(); // Record of how long since each cell last split. 
        private SciRand rnd = new();

        [JsonProperty]      // This makes the dropdown selection saveable.
        [InstanceChooser(label = "Input receptor")] // This creates a dropdown of all instances of the type
                                                    // (Receptor here), so you can choose the one to plug in.
        public Receptor Input;

        [JsonProperty]      // This makes the dropdown selection saveable.
        [InstanceChooser(label = "Second input")] // This creates a dropdown of all instances of the type
                                                  // (Receptor here), so you can choose the one to plug in.
        public Receptor Second_Input;

        [Param(Name = "First input weight", Min = 0, Max = 1)]
        public double first_weight { get; set; } = 1;

        [Param(Name = "Second input weight", Min = 0)]
        public double second_weight { get; set; } = 0.5;

        [JsonProperty]      // This makes the dropdown selection saveable.
        [InstanceChooser(label = "Output ligand")] // This creates a dropdown of all instances of the type
                                                    // (Receptor here), so you can choose the one to plug in.
        public Ligand Output;



        [Param(Name = "Threshold occupancy", Min = 0, Max = 1)]
        public double threshold { get; set; } = 0.6;

        [Param(Name = "Full rate occupancy", Min = 0, Max = 1)]
        public double max_threshold { get; set; } = 0.9;

        [Param(Name = "Pulse time", Min = 0)]
        public double wavelength { get; set; } = 1;

        [Param(Name = "Pulse amplitude", Min = 0, Max = 1)]
        public double amplitude { get; set; } = 0.025;

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
            if (lastPulse[cell] < wavelength)
            {
                lastPulse[cell] += sim.Settings.dt; //If we haven't given the cell time
                                                      //to recover from its last split, wait.
                return;
            }

            double stimulus = 0;
            if (this.Input != null) stimulus += first_weight * cell.ReceptorActivity(this.Input);

            if (this.Second_Input != null)
            {
                stimulus += second_weight * cell.ReceptorActivity(this.Second_Input);
            }
            double mult = 1d / env.settings.DX;


            if(stimulus > threshold) // If we're over the threshold
            {
                bool do_pulse;
                lock (rnd)
                {
                    do_pulse = sim.Settings.dt * (stimulus - threshold) * (stimulus - threshold) / ((max_threshold - threshold)* (max_threshold - threshold)) > rnd.value;
                }
                if (do_pulse)
                {
                    double current;
                    foreach (Point p in cell.localPoints)
                    {
                        current = (Output!=null) ? env.GetConcentration(Output, p.X, p.Y) : 0;
                        env.SetConcentration((int)Math.Round(p.X * mult), (int)Math.Round(p.Y * mult), Output, current+amplitude);
                    }
                    lastPulse[cell] = 0;
                }
            }
        }

        private void RegisterNewCell(Simulation sim, Cell cell)
        {
            if (!lastPulse.ContainsKey(cell)) lastPulse.Add(cell, 0);
        }
    }
}