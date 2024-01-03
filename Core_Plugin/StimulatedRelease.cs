using System;
using WPF_Chemotaxis.Model;
using WPF_Chemotaxis.Simulations;
using WPF_Chemotaxis.UX;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Windows;
using WPF_Chemotaxis.VisualScripting;
using ILGPU.Runtime.Cuda;
using ILGPU.Backends.PTX;
using static WPF_Chemotaxis.Simulations.Environment;

namespace WPF_Chemotaxis.CorePlugin
{
    [VSElement(ui_TypeLabel = "Ligand release", symbolResourcePath = "Core_Plugin;component/Resources/ChannelIcon.png", symbolSize = 6.0, tagX = 12, tagY = -12, tagCentre = false)]
    [LineConnector]
    public class StimulatedRelease : LabelledLinkable, ICellComponent // Labelled linkable already does the heavy
                                                                      // lifting to put this in the GUI. ICellComponent means
                                                                      // it can be added to a cell to do extra logic.
    {
        private Dictionary<Cell, double> lastPulse = new(); // Record of how long since each cell last split. 
        private SciRand rnd = new();

        [VisualLine(parentAnchor = LineAnchorType.ANCHOR_FORWARD, childAnchor = LineAnchorType.ANCHOR_CENTRE, parentAnchorDistance = 25.0, childAnchorDistance = 18.0, parentArrowHeadFunc="GetArrowhead", childArrowHead = LineHeadType.ARROW)]
        [JsonProperty]      // This makes the dropdown selection saveable.
        [InstanceChooser(label = "Output ligand")] // This creates a dropdown of all instances of the type
                                                   // (Receptor here), so you can choose the one to plug in.
        public Ligand Output { get; set; } = null;


        [Param(Name = "Threshold occupancy", Min = 0, Max = 1)]
        public double threshold { get; set; } = 0.6;

        [Param(Name = "Full rate occupancy", Min = 0, Max = 1)]
        public double max_threshold { get; set; } = 0.9;

        [Param(Name = "Pulse time", Min = 0)]
        public double wavelength { get; set; } = 1;

        [Param(Name = "Pulse amplitude", Min = 0)]
        public double amplitude { get; set; } = 0.025;

        public StimulatedRelease () : base() 
        {
            Init();
        }
        public StimulatedRelease (string name) : base(name)
        {
            Init();
        }

        [ElementAdder(label = "Add Ligand", type = typeof(Ligand))]
        public void AddLigand(Ligand ligand)
        {
            if (Output != ligand)
            {
                Output = ligand;
            }
        }
        protected LineHeadType GetArrowhead()
        {
            return LineHeadType.CIRCLE;
        }

        private Simulation.CellNotificationHandler RegisterCell; // We don't know how many components cells will have,
                                                          // or how many cells a simulation will have, so we
                                                          // deal with their initialisation using events and
                                                          // then components subscribe to any they are interested
                                                          // in. This is a handler for a new cell being added.

        public void Initialise(Simulation sim) //Initialise is a required part of a cell component.
                                               //Called once when the simulation starts.
        {
            sim.CellAdded += RegisterNewCell; // We have used it to subscribe to the
                                                                            // "Cell Added" event in the simulation
        }

        // Update called every dt!
        public void Update(Cell cell, Simulation sim, WPF_Chemotaxis.Simulations.Environment env) 
        {
            if (lastPulse.TryGetValue(cell, out var last))
            {
                if (last < wavelength)
                {
                    lastPulse[cell] += sim.Settings.dt; //If we haven't given the cell time
                                                        //to recover from its last split, wait.
                    return;
                }

                double stimulus = cell.WeightedActiveReceptorFraction;
                double mult = 1d / env.settings.DX;

                if (stimulus > threshold) // If we're over the threshold
                {
                    bool do_pulse;
                    lock (rnd)
                    {
                        do_pulse = sim.Settings.dt * (stimulus - threshold) * (stimulus - threshold) / ((max_threshold - threshold) * (max_threshold - threshold)) > rnd.value;
                    }
                    if (do_pulse)
                    {
                        if (Output != null)
                        {
                            double current;
                            foreach (Point p in cell.localPoints)
                            {
                                current = env.GetConcentration(Output, p.X, p.Y);

                                int x = (int)Math.Round(p.X * mult);
                                int y = (int)Math.Round(p.Y * mult);

                                if (env.GetFlag(x, y, PointType.FIXED)) continue;
                                else env.SetConcentration(x, y, Output, current + amplitude);
                            }
                            lastPulse[cell] = 0;
                        }
                    }
                }
            }
        }

        private void RegisterNewCell(Simulation sim, CellNotificationEventArgs e)
        {
            
            if (!lastPulse.ContainsKey(e.NewCell)) lastPulse.Add(e.NewCell, 0);
        }

        public void ConnectToCellType(CellType ct)
        {
            new ExpressionCoupler(this, ct);
        }
    }
}
