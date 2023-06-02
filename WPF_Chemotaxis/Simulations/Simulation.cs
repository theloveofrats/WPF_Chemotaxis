using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Chemotaxis.Model;
using WPF_Chemotaxis.UX;
using System.IO;
using System.Diagnostics;

namespace WPF_Chemotaxis.Simulations
{
    /// <summary>
    /// Instances of Simulation are the core element of running simulations, informing other elsements of updates, changes to cell number
    /// and integrating the behaviour of Cell instances with the environment to properly update cell position. Having said this, further
    /// interaction with the class is probably best kept passive, say by subscribing to its events!
    /// </summary>
    public class Simulation : IDisposable
    {
        private SimulationSettings settings;
        public SimulationSettings Settings
        {
            get
            {
                return settings;
            }
        }

        public static Simulation Current { get; private set; }

        private int nextCellNum = 0;

        private double time = 0;
        public double Time
        {
            get
            {
                return time;
            }
        }
        public int W
        {
            get
            {
                return this.environment.Width;
            }
        }
        public int H
        {
            get
            {
                return this.environment.Height;
            }
        }
        public Environment Environment
        {
            get
            {
                return this.environment;
            }
        }

        private bool paused = true;
        private bool cancelled = false;

        public delegate void SimulationNotification(Simulation sim, Environment env, SimulationNotificationEventArgs e);
        public delegate void CellNotificationHandler(Simulation sim, CellNotificationEventArgs e);

        public event CellNotificationHandler CellAdded;
        public event CellNotificationHandler CellRemoved;
        public event CellNotificationHandler CellReplaced;

        public event SimulationNotification Redraw;
        //Update before mass transport equations
        public event SimulationNotification EarlyUpdate;
        //Main update
        public event SimulationNotification Update;
        //Update after collision detection?- this isn't in yet!
        public event SimulationNotification LateUpdate;

        public event SimulationNotification WriteToFile;
        public event SimulationNotification Close;

        private HashSet<Cell> cells = new();
        public  IReadOnlyCollection<Cell> Cells { get
            {
                return cells;
            } 
        }
        private HashSet<Cell> newCells = new();
        private Dictionary<Cell, CellEventType> removedcells = new();
        private Environment environment;

        private Stopwatch draw_watch = new();
        private SimulationNotificationEventArgs defaultEventArgs;

        private IFluidModel fluid;
        private bool disposedValue;

        SimulationSettings.UIEventHandler pauseSub;
        SimulationSettings.UIEventHandler resumeSub;

        StreamWriter writer;
        string targetDirectory;
        public string TargetDirectory
        {
            get
            {
                return targetDirectory;
            }
        }

        public static Simulation StartSimulation(SimulationSettings settings, EnvironmentSettings envSettings, string targetDirectory)
        {
            if (Current == null || Current.disposedValue)
            {
                Current = new Simulation(settings, envSettings, targetDirectory);
            }
            return Current;
        }
        private Simulation(SimulationSettings settings, EnvironmentSettings envSettings, string targetDirectory)
        {
            this.settings = settings;

            pauseSub = (o, e) => paused = true;
            resumeSub = (o, e) => paused = false;

            this.settings.Pause += pauseSub;
            this.settings.Resume += resumeSub;
            this.defaultEventArgs = new(settings.dt) { };
            this.environment = new Environment(envSettings);
            Initialise(targetDirectory??"");
        }
        /// <summary>
        /// Builds a thread with the Simulation "Run" task. Should only happen once for each simulation instance.
        /// </summary>
        public void Start()
        {
            Thread thread = new Thread(new ThreadStart(this.Run)); 
            thread.Start();
        }
        public void Cancel()
        {
            this.cancelled = true;
        }
        private void Run()
        {
            Iterate();
            draw_watch.Start();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (time<settings.duration && !cancelled)
            {
                if (this.paused)
                {
                    Thread.Sleep(500);
                    continue;
                }
                else
                {
                    this.Iterate();
                }
            }
            if (this.Close != null)
            {
                this.Close(this,environment, new SimulationNotificationEventArgs(dt:settings.dt));
            }
            watch.Stop();
            System.Diagnostics.Debug.Print(string.Format("Duration: {0}", watch.Elapsed.ToString(@"mm\:ss\:ff")));
            this.Dispose();
        }

        private void Initialise(string newTargetDirectory)
        {
            this.targetDirectory = newTargetDirectory;
            if (Directory.Exists(targetDirectory))
            {
                writer = new StreamWriter(targetDirectory + "Cells.csv", false);
                writer.WriteLineAsync("Time (min), Cell Type, Cell ID, X (um), Y (um), Mean receptor activity");
            }

            // This is a dirty, undesireable way of doing it, because it's so poorly extensible.
            // There should be a sim start event from, say, the simsettings, but this does give everyone a chance to subscribe to what they need to.
            
            foreach (ILinkable link in Model.Model.MasterElementList)
            {
                if(link is CellType)
                {
                    CellType ct = link as CellType;
                    foreach (var component in ct.components) component.Initialise(this);
                    if(ct.drawHandler!=null) ct.drawHandler.Initialise(this);
                }
            }
            //Environment initialised last so that the addition of cells can be intercepted by the correct modules.
            environment.Init(this);
        }

        private void Iterate()
        {
            //Early update. Any preparatory activity needed.
            if (this.EarlyUpdate != null)
            {
                Parallel.ForEach(EarlyUpdate.GetInvocationList(), action =>
                {
                    (action as SimulationNotification).Invoke(this, environment, defaultEventArgs);
                });
                //EarlyUpdate(this, this.environment, defaultEventArgs);
            }
            //Main update. Most things happen here. 
            environment.Update(settings.dt);

            if (this.Update != null)
            {
                Parallel.ForEach(Update.GetInvocationList(), action =>
                {
                    (action as SimulationNotification).Invoke(this, environment, defaultEventArgs);
                });
                //Update(this, this.environment, defaultEventArgs);

            }
            
            foreach (Cell c in cells)
            {
                TryMoveCell(c);
            }
            if (Redraw != null) {
                if (draw_watch.ElapsedTicks > 300000)
                {
                    Redraw(this, this.environment, defaultEventArgs);
                    draw_watch.Restart();
                }
            }

            foreach(Cell cell in removedcells.Keys)
            {
                cells.Remove(cell);
                if (this.CellRemoved != null)
                {
                    this.CellRemoved(this, new CellNotificationEventArgs(eventType:removedcells[cell], oldCell:cell, newCell:null));
                }
            }
            removedcells.Clear();

            //Late update- clearing up, depenent calculations &c.
            if (this.LateUpdate != null)
            {
                Parallel.ForEach(LateUpdate.GetInvocationList(), action =>
                {
                    (action as SimulationNotification).Invoke(this, environment, defaultEventArgs);
                });
                //Update(this, this.environment, defaultEventArgs);}

                time += settings.dt;
            }
            if (settings.out_freq > 0)
            {
                if (time % settings.out_freq < settings.dt)
                {
                    WriteCellPositionData();
                    if (this.WriteToFile != null)
                    {
                        WriteToFile(this, this.environment, defaultEventArgs);
                    }
                }
            }
            foreach (Cell cell in newCells)
            {
                cells.Add(cell);
            }
            newCells.Clear();
        }

        private void WriteCellPositionData()
        {
            foreach (Cell cell in cells)
            {
                writer.WriteLine(string.Format("{0:0.000}, {1}, {2}, {3:0.000}, {4:0.000}, {5:0.000}", Time, cell.CellType.Name, cell.Id, cell.X, cell.Y, cell.WeightedActiveReceptorFraction));
            }
        }

        /// <summary>
        /// Method for adding a new cell to the simulation. Will trigger the CellAdded event.
        /// </summary>
        /// <param name="ct">The CellType for the new cell</param>
        /// <param name="x">The micrometer x position of the new cell</param>
        /// <param name="y">The micrometer y position of the new cell</param>
        public void AddCell(CellType ct, double x, double y, CellEventType addedHow)
        {
            lock (newCells)
            {
                Cell cell = new Cell(ct, nextCellNum++, x, y, this);
                newCells.Add(cell);
                if (CellAdded != null)
                {
                    CellAdded(this, new CellNotificationEventArgs(eventType:addedHow,oldCell:null, newCell:cell));
                }
            }
        }

        /// <summary>
        /// Queues a cell for removal at the end of the current tick.  
        /// </summary>
        /// <param name="c">The cell to remove</param>
        /// <param name="deathType">Specified deathtype, which may be used by other components to initiate custom behaviour.</param>
        public void RemoveCell(Cell c, CellEventType deathType)
        {
            removedcells.TryAdd(c, deathType);
        }

        public void ReplaceCell(CellType newCellType, Cell oldCell)
        {
            Cell newCell = new Cell(newCellType, oldCell.Id, oldCell.X, oldCell.Y, this);
            removedcells.TryAdd(oldCell, CellEventType.DIFFERENTIATED);
            newCells.Add(newCell);
            if (this.CellReplaced != null)
            {
                this.CellReplaced(this, new CellNotificationEventArgs(CellEventType.DIFFERENTIATED, oldCell, newCell));
            }
        }

        // We might consider referring movement back to the cell upon error, this isn't very SOLID.
        private bool TryMoveCell(Cell cell)
        {
            double dvx = cell.vx * settings.dt;
            double dvy = cell.vy * settings.dt;

            //System.Diagnostics.Debug.Print(string.Format("TryMoveCell with vx={0}, vy={1}", cell.vx, cell.vy));

            if (!environment.IsOpen(cell.X + dvx, cell.Y + dvy)||environment.Blocked(cell.X + dvx, cell.Y + dvy))
            {
                dvx *= -0.25;
                dvy *= -0.25;

                if (!environment.IsOpen(cell.X + dvx, cell.Y+dvy) || environment.Blocked(cell.X + dvx, cell.Y + dvy))
                {
                    cell.vx = 0;
                    cell.vy = 0;
                    return false;
                }
            }

            cell.UpdateIntendedMovementDirection(dvx, dvy);

            cell.UpdatePosition(cell.X+cell.vx, cell.Y +cell.vy);
            return true;
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach(Cell c in cells)
                    {
                        c.Dispose();
                    }
                    if (environment != null)
                    {
                        environment.Dispose();
                    }
                }
                if (this.writer != null)
                {
                    this.writer.Flush();
                    this.writer.Dispose();
                }
                this.settings.Pause -= this.pauseSub;
                this.settings.Resume -= this.resumeSub;
                CellAdded = null;
                CellRemoved = null;
                this.Close = null;
                this.Redraw = null;
                this.EarlyUpdate = null;
                this.LateUpdate = null;
                this.cells.Clear();
                this.newCells.Clear();
                this.environment = null;
                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                disposedValue = true;
                Simulation.Current = null;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Simulation()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
