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

namespace WPF_Chemotaxis.Simulations {
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

        public delegate void SimulationNotification(Simulation sim, Environment e, IEnumerable<Cell> cells);
        public delegate void CellNotificationHandler(Simulation sim, Cell cell, CellNotificationEventArgs e);

        public event CellNotificationHandler CellAdded;
        public event CellNotificationHandler CellRemoved;

        public event SimulationNotification Redraw;
        public event SimulationNotification EarlyUpdate;
        public event SimulationNotification LateUpdate;
        public event SimulationNotification WriteToFile;
        public event SimulationNotification Close;

        private HashSet<Cell> cells = new();
        private Dictionary<Cell, CellDeathType> removedcells = new();
        private Environment environment;

        private Stopwatch draw_watch = new();


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
                this.Close(this,environment,cells);
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

                    foreach (ICellComponent component in ct.components) component.Initialise(this);
                    if(ct.drawHandler!=null) ct.drawHandler.Initialise(this);
                }
            }
            environment.Init(this);
        }

        private void Iterate()
        {
            //Early update. Any preparatory activity needed.
            if(this.EarlyUpdate!=null) EarlyUpdate(this,this.environment, cells);

            //Main update. Most things happen here. 
            environment.Update(settings.dt);
            
            Parallel.ForEach(cells, c =>
            {
                c.UpdateInformation(this, this.environment, this.fluid, settings.dt);
            });

            Parallel.ForEach(cells, c =>
            {
                c.PerformInteractions(this.environment, this.fluid, settings.dt);
            });
            
            foreach (Cell c in cells)
            {
                TryMoveCell(c);
            }
            if (Redraw != null) {
                if (draw_watch.ElapsedTicks > 300000)
                {
                    Redraw(this, this.environment, this.cells);
                    draw_watch.Restart();
                }
            }

            foreach(Cell cell in removedcells.Keys)
            {
                cells.Remove(cell);
                if (this.CellRemoved != null)
                {
                    this.CellRemoved(this, cell, new CellNotificationEventArgs() { DeathType = removedcells[cell] });
                }
            }
            removedcells.Clear();

            //Late update- clearing up, depenent calculations &c.
            if (this.LateUpdate != null) LateUpdate(this, this.environment, cells);

            time += settings.dt;
            if (settings.out_freq > 0)
            {
                if (time % settings.out_freq < settings.dt)
                {
                    WriteCellPositionData();
                    if (this.WriteToFile != null)
                    {
                        WriteToFile(this, this.environment, this.cells);
                    }
                }
            }
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
        public void AddCell(CellType ct, double x, double y)
        {
            Cell cell = new Cell(ct, nextCellNum++, x, y, this);
            cells.Add(cell);
            if (CellAdded != null)
            {
                CellAdded(this, cell, new CellNotificationEventArgs());
            }
        }

        /// <summary>
        /// Queues a cell for removal at the end of the current tick.  
        /// </summary>
        /// <param name="c">The cell to remove</param>
        /// <param name="deathType">Specified deathtype, which may be used by other components to initiate custom behaviour.</param>
        public void RemoveCell(Cell c, CellDeathType deathType)
        {
            removedcells.TryAdd(c, deathType);
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
        /// <summary>
        /// The currently active Cell instance collection.
        /// </summary>
        public ICollection<Cell> Cells
        {
            get
            {
                return cells;
            }
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
