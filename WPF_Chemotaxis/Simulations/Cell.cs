﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using WPF_Chemotaxis.Model;
using System.Windows.Media;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using LiveCharts;
using static WPF_Chemotaxis.Simulations.Simulation;

namespace WPF_Chemotaxis.Simulations
{
    /// <summary>
    /// The representation of an actual cell in a simulation. It has a CellType, which provides it with its 
    /// relationships and parameter options, and deals with local information specific to the instance,
    /// for example its actual position, the grid squares it has contact with, the occupancy and activity
    /// of its receptors. Cell behaviour beyond this passive environmental sensing is done using ICellComponents,
    /// which can modify base values for things like receptor activity, and call tell cells to try to move.
    /// </summary>
    public class Cell : IDisposable, IGraphOnSelection
    {
        private CellType cellType;
        public CellType CellType
        {
            get
            {
                return cellType;
            }
        }

        private int id;
        /// <summary>
        /// Unique ID number. Each new cell increments the value.
        /// </summary>
        public int Id
        {
            get
            {
                return id;
            }
        }
        /// <summary>
        /// Sum over all receptors r (weight_r * activity_r)
        /// </summary>
        public double WeightedActiveReceptorFraction
        {
            get
            {
                double num = 0;
                double val = 0;
                foreach (Receptor r in receptorActivities.Keys)
                {
                    num++;
                    val += ReceptorActivity(r) * ReceptorWeight(r);
                }
                if (num == 0) return 0;
                val /= num;
                return val;
            }
        }

        public double WeightedActiveReceptorFractionNonNegative
        {
            get
            {
                double num = 0;
                double val = 0;
                double new_val = 0;
                foreach (Receptor r in receptorActivities.Keys)
                {
                    new_val = ReceptorActivity(r) * ReceptorWeight(r);
                    if (new_val >= 0) 
                    {
                        num++;
                        val += new_val;
                    }
                }
                if (num == 0) return 0;

                val /= num;
                return val;
            }
        }

        private ICollection<Point> localPointCoordinates = new List<Point>();
        /// <summary>
        /// Returns a list of Points currently occupied by the cell
        /// </summary>
        public ICollection<Point> localPoints
        {
            get
            {
                return localPointCoordinates;
            }
        }
        private double[] centre = new double[2];
        public double[] Centre
        {
            get
            {
                return centre;
            }
        }

        public double vx { get; set; }
        public double vy { get; set; }

        private double x;
        private double y;


        private double speed = 0;
        /// <summary>
        /// Returns the working cell Speed, modified by ICellComponents.
        /// </summary>
        public double Speed {
            get {
                double modified = speed;
                foreach(ICellComponent comp in cellType.components)
                {
                    comp.ModifySpeed(this, speed, ref modified);
                }
                return modified;
            }
            private set {
                this.speed = value;
            }
        }

        /// <summary>
        /// Returns the weight of Receptor r after ICellComponent modification.
        /// </summary>
        public double ReceptorWeight(Receptor r)
        {
            if (receptorWeights.ContainsKey(r))
            {
                double baseval = receptorWeights[r];
                double modified = baseval;
                foreach (ICellComponent comp in cellType.components)
                {
                    comp.ModifyReceptorWeight(this, r, baseval, ref modified);
                }
                return modified;
            }
            return 0;
        }
        /// <summary>
        /// Returns the activity of Receptor r after ICellComponent modification.
        /// </summary>
        public double ReceptorActivity(Receptor r)
        {
            if (r!=null && receptorActivities.ContainsKey(r))
            {
                double baseval = receptorActivities[r];
                double modified = baseval;
                foreach (ICellComponent comp in cellType.components)
                {
                    comp.ModifyReceptorActivity(this, r, baseval, ref modified);
                }
                return modified;
            }
            return 0;
        }
        /// <summary>
        /// Returns the occupancy difference across Receptor r after ICellComponent modification.
        /// </summary>
        public Vector ReceptorDifference(Receptor r)
        {
            if (receptorDifferences.ContainsKey(r))
            {
                Vector basevec = receptorDifferences[r];
                Vector modified = basevec;
                foreach (ICellComponent comp in cellType.components)
                {
                    comp.ModifyReceptorDifference(this, r,basevec, ref modified);
                }

                //System.Diagnostics.Debug.Print(string.Format("Receptor difference ({0:0.00},{1:0.00}) from base ({2:0.00},{3:0.00})", modified.X, modified.Y, basevec.X, basevec.Y));
                return modified;
            }
            //System.Diagnostics.Debug.Print("No such receptor");
            return new Vector(0,0);
        }

        /// <summary>
        /// The actual radius of the cell, randomly sampled from the range of values in CellType
        /// </summary>
        public double radius { get; private set; } = 5;

        private Dictionary<Receptor, double> receptorWeights;
        private Dictionary<Receptor, double> receptorActivities;
        private Dictionary<Receptor, Vector> receptorDifferences;
        private bool disposedValue;
        private PropertyChangedEventHandler propSubHandler;


        /// <summary>
        /// The micrometer x position of the cell
        /// </summary>
        public double X
        {
            get
            {
                return x;
            }
        }
        /// <summary>
        /// The micrometer Y position of the cell
        /// </summary>
        public double Y
        {
            get
            {
                return y;
            }
        }

        /*public double ReceptorActivity(Receptor r)
        {
            double val;

            if (receptorActivities.TryGetValue(r, out val))
            {
                return val;
            }
            else return 0;
        }
        public double ReceptorWeight(Receptor r)
        {
            double res;
            if (receptorWeights.TryGetValue(r, out res))
            {
                return res;
            }
            return 0;
        }
        public Vector ReceptorDifference(Receptor r)
        {
            Vector dir;
            if (receptorDifferences.TryGetValue(r, out dir))
            {
                return dir;
            }
            return default(Vector);
        }*/

        /// <summary>
        /// Tells the cell to generate new specific instances of speed and radius within 
        /// the ranges specified by CellType.
        /// </summary>
        private void UpdateParameters(CellType cellType)
        {
            this.speed = cellType.baseSpeed.RandomInRange;
            this.radius = cellType.radius.RandomInRange;
        }

        private void Init(Simulation sim)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate {
                series = new()
                {
                    Values = new ChartValues<ObservablePoint>(),
                    Fill = Brushes.Transparent,
                    LineSmoothness = 0.25,
                    ToolTip = null,
                    StrokeThickness = 5d
                };
            });

            UpdateParameters(cellType);
            propSubHandler = (s, e) => this.UpdateParameters(cellType);
            cellType.PropertyChanged += propSubHandler;

            receptorWeights = new();
            receptorActivities = new();
            receptorDifferences = new();
            foreach (ExpressionCoupler crr in this.cellType.receptorTypes)
            {
                Receptor rec = crr.ChildComponent as Receptor;
                if (rec == null) continue;
                receptorWeights.Add(rec, crr.BasalWeight.RandomInRange);
                receptorActivities.Add(rec, 0);
            }
            UpdateLocalRegion(sim.Environment);
        }

        public Cell(CellType cellType, int cellNumber, double x, double y, Simulation sim)
        {
            this.cellType = cellType;
            this.x = x;
            this.y = y;
            this.vx = 0;
            this.vy = 0;
            this.id = cellNumber;

            sim.EarlyUpdate += this.UpdateInformation;
            sim.Update += this.PerformInteractions;
            sim.CellRemoved += this.CheckRemoveCell;

            Init(sim);
            if (this.cellType.drawHandler == null)
            {
                this.cellType.SetDrawHandler(Activator.CreateInstance(typeof(CellDrawHandler_BasicCircle)) as ICellDrawHandler);
            }
        }

        public void CheckRemoveCell(Simulation sim, CellNotificationEventArgs args)
        {
            if (args.OldCell == this)
            {
                sim.EarlyUpdate -= this.UpdateInformation;
                sim.Update -= this.PerformInteractions;
                sim.CellRemoved -= this.CheckRemoveCell;
            }
        }

        /// <summary>
        /// Draws the cell on the simulation overlay canvas.
        /// </summary>
        public void Draw(Simulations.Environment env, WriteableBitmap targetCanvas)
        {
            CellType.drawHandler.Draw(this, env, targetCanvas, localPoints);
        }

        /// <summary>
        /// Sets the actual position of the cell- best used only by Simulation.
        /// </summary>
        public void UpdatePosition(double newX, double newY)
        {
            this.x = newX;
            this.y = newY;
        }

        /// <summary>
        /// Sets the intended direction of motion of the cell. May be overruled if it,
        /// say, were to crash into a wall.
        /// </summary>
        public void UpdateIntendedMovementDirection(double vx, double vy)
        {
            this.vx = vx;
            this.vy = vy;
        }

        /// <summary>
        /// Caches the local coordinate list for this iteration/update
        /// </summary>
        /// <param name="environment">the Environment from which to read available points</param>
        public virtual void UpdateLocalRegion(Environment environment)
        {
            double x = X;
            double y = Y;

            double cx = 0, cy = 0;

            //Note- first release doesn't matter because no points to release!

            //Local area dilates by 1 extra dx for cells that are approx the size of dx
            lock (localPoints)
            {
                double dx = environment.settings.DX;
                localPointCoordinates.Clear();
                for (double i = x - (radius+dx); i <= x + (radius+dx); i += dx)
                {
                    for (double j = y - (radius + dx); j <= y + (radius + dx); j += dx)
                    {
                        if ((i - x) * (i - x) + (j - y) * (j - y) < (radius + dx) * (radius + dx) && environment.IsOpen(i, j))
                        {
                            localPointCoordinates.Add(new Point(i, j));

                            cx += i;
                            cy += j;
                        }
                    }
                }
                //environment.OccupyPoints(localPointCoordinates);
                //environment.ReleasePoints(localPointCoordinates);
            }
            cx /= localPointCoordinates.Count;
            cy /= localPointCoordinates.Count;

            centre[0] = cx;
            centre[1] = cy;
        }

        /// <summary>
        /// Caches the receptor occupancy/activity/differences for this iteration/update
        /// </summary>
        /// <param name="environment">the Environment from which to read ligand concentrations</param>
        protected virtual void UpdateReceptorState(Environment environment)
        {
            /*double cx = 0, cy = 0;
            foreach (Point p in localPoints)
            {
                cx += p.X;
                cy += p.Y;
            }
            cx /= localPoints.Count;
            cy /= localPoints.Count;*/
            // double x = Centre[0];
            // double y = Centre[1];



            double eff, receptor_mean_eff, receptor_moment_x, receptor_moment_y;

            if (CellType.receptorTypes.Count() > 0)
            {
                foreach (Receptor r in receptorActivities.Keys)
                {
                    if (r.ligandInteractions.Count == 0) continue;

                    receptor_mean_eff = receptor_moment_x = receptor_moment_y = 0;
                    foreach (Point p in localPoints)
                    {
                        eff = r.GetEfficacy(environment, p.X, p.Y);
                        receptor_mean_eff += eff;
                        receptor_moment_x += eff * (p.X - centre[0]);
                        receptor_moment_y += eff * (p.Y - centre[1]);
                    }
                    receptor_mean_eff /= localPoints.Count;
                    receptor_moment_x /= localPoints.Count;
                    receptor_moment_y /= localPoints.Count;

                    receptorActivities[r] = receptor_mean_eff;
                    receptorDifferences[r] = new Vector(receptor_moment_x, receptor_moment_y);
                }
            }
        }

        /// <summary>
        /// Cell early update. Updates local region and receptor state. 
        /// </summary>
        /// <param name="sim">the Environment from which to read ligand concentrations within the cell</param>
        /// <param name="environment">the Environment from which to read ligand concentrations within the cell</param>
        /// <param name="fluidModel"> the fluid model describing local advection (currently not implemented!)</param>
        /// <param name="dt"> the timestep</param>
        public virtual void UpdateInformation(Simulation sim, Environment environment, SimulationNotificationEventArgs e)
        {
            //UpdateLocalRegion(environment);
            UpdateReceptorState(environment);
            currentTime = sim.Time;
            //TODO- the comps should subscribe to event they want to be part of, really.
            foreach (ICellComponent comp in CellType.components) comp.Update(this, sim, environment);
        }
        /// <summary>
        /// Cell early update. Updates local region and receptor state. 
        /// </summary>
        /// <param name="sim">the Environment from which to read ligand concentrations within the cell</param>
        /// <param name="environment">the Environment from which to read ligand concentrations within the cell</param>
        /// <param name="fluidModel"> the fluid model describing local advection (currently not implemented!)</param>
        /// <param name="dt"> the timestep</param>
        public virtual void PerformInteractions(Simulation sim, Environment environment, SimulationNotificationEventArgs e) {
            foreach (CellLigandRelation clr in CellType.ligandInteractions)
            {
                clr.DoUpdateAction(environment, this, e.dt);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.receptorWeights.Clear();
                    this.receptorWeights = null;
                    cellType.PropertyChanged -= propSubHandler;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;

            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Cell()
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

        public bool IsAtPosition(double x, double y)
        {
            return ((x - this.x) * (x - this.x) + (y - this.y) * (y - this.y) < 1.25 * this.radius * this.radius);
        }

        public void DrawHighlight(Simulation sim, WriteableBitmap bmp, Color clr)
        {
            double dx = sim.Environment.settings.DX;
            double scale = bmp.PixelWidth * 1.0 / (dx * sim.Environment.Width);

            WriteableBitmapExtensions.FillEllipseCentered(bmp, (int)Math.Round(scale * this.X), (int)Math.Round(scale * this.Y), (int)Math.Round(1.8 * scale * this.radius), (int)Math.Round(1.8 * scale * this.radius), clr);
        }

        private LineSeries series;

    
        private double currentTime = 0;
        public LineSeries GetValues()
        {
            series.Values.Add(new ObservablePoint(currentTime, this.WeightedActiveReceptorFraction));
            return series;
        }
    }
}
