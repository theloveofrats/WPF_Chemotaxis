using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using WPF_Chemotaxis.Model;
using WPF_Chemotaxis.UX;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;
using ILGPU.Runtime.CPU;
using System.Diagnostics;

namespace WPF_Chemotaxis.Simulations
{
    /// <summary>
    /// The Environment class encapsulates aspects of the simulation that relate to layout, local rules and
    /// current distributions of ligands (or other chemical agents). It is the model class for the device the 
    /// experiment is performed in, and the places that attractant and cells have been inserted with a pippette.
    /// </summary>
    public class Environment : IDisposable
    {
        public EnvironmentSettings settings;

        //GPU elements
        protected Context context;
        protected Accelerator accelerator;
        private bool forceCPU = false;

        protected MemoryBuffer1D<double, Stride1D.Dense> kernel_rxn;
        protected MemoryBuffer1D<double, Stride1D.Dense> kernel_rx2;
        protected MemoryBuffer1D<double, Stride1D.Dense> kernel_cm1;
        protected MemoryBuffer1D<double, Stride1D.Dense> kernel_c;
        protected MemoryBuffer1D<double, Stride1D.Dense> kernel_cp1;
        protected MemoryBuffer1D<double, Stride1D.Dense> kernel_dif;
        protected MemoryBuffer1D<double, Stride1D.Dense> kernel_tmp;
        protected MemoryBuffer1D<byte, Stride1D.Dense> kernel_mask;

        protected Action<Index1D, ArrayView<double>, ArrayView<double>, ArrayView<double>, ArrayView<byte>, ArrayView<double>, int, ArrayView<double>, double, int, int> loadedKernel;
        protected Action<Index1D, ArrayView<double>, ArrayView<double>, ArrayView<double>, ArrayView<byte>, ArrayView<double>, int, double, int, int> reactionKernel;
        protected Action<Index1D, ArrayView<double>, ArrayView<double>, ArrayView<double>> swapKernel;

        protected double[] rxn_full;
        protected double[] cm1_full;
        protected double[] c00_full;
        protected double[] cp1_full;

        [Flags]
            public enum PointType
            {
                NONE = 0,
                FREE = 1,  // To be simulated. Not-free points are entirely ignored.
                FIXED = 2, // reservoir. Concentration cannot change through advection-diffusion
                BLOCK = 4, // Blocks cells, not diffusion
                STAGE = 8, // Cells will start (or be spawned) here.
            }

        private HashSet<Region> regions = new();
        /// <summary>
        /// Returns a collection of specific regions (contiguous areas of a single colour in the target PNG). 
        /// </summary>
        public ICollection<Region> Regions
        {
            get
            {
                return regions;
            }
        }

        /// <summary>
        /// The environment width in points. Multiply by EnvironmentSettings.DX for a micrometer width. 
        /// </summary>
        private int width;
        public int Width
        {
            get
            {
                return width;
            }
        }
       
        private int height;
        /// <summary>
        /// The environment height in points. Multiply by EnvironmentSettings.DX for a micrometer height. 
        /// </summary>
        public int Height
        {
            get
            {
                return height;
            }
        }
        private double pre_k;
        private List<Rect> selectedAreas;

        private byte[] pointTypes;
        private int[]  occupiedSites;

        private Dictionary<Ligand, double[]> reactions = new();
        private double[] reactions_new;
        private Dictionary<Ligand, double[]> c_m1 = new();
        private Dictionary<Ligand, double[]> c_p1 = new();
        private Dictionary<Ligand, double[]> c    = new();
        private Dictionary<Ligand, int> ligand_order = new();
        private int num_ligands;

        private double[] EMPTY = new double[] { };
        private bool disposedValue;

        public Environment(EnvironmentSettings settings)
        {
            this.settings = settings;
        }

        /// <summary>
        /// The environment height in points. Multiply by EnvironmentSettings.DX for a micrometer height. 
        /// </summary>
        /// <param name="x">x position to set, in points</param>
        /// <param name="y">y position to set, in points</param>
        /// <param name="type">the PointType value to be altered</param>
        /// <param name="val">target value for the PointType (true or false)</param>
        public void SetFlag(int x, int y, PointType type, bool val)
        {
            int psn = width * y + x;
            if (val) 
            {
                pointTypes[psn] = (byte) (pointTypes[psn] | (byte) type);
            }
            else
            {
                pointTypes[psn] = (byte)(pointTypes[psn] & ~(byte)type);
            }
        }

        public void SetSelectedAreas(List<Rect> selections)
        {
            this.selectedAreas = selections;
        }
        public void ClearSelections()
        {
            this.selectedAreas.Clear();
        }
        /// <summary>
        /// Gets the value of the flag for PointType type at point position (not micrometer position) x,y. 
        /// </summary>
        public bool GetFlag(int x, int y, PointType type)
        {
            int psn = width * y + x;
            byte pointType = (byte)type;

            return (this.pointTypes[psn] & pointType) == pointType;
        }

        public bool IsInSelection(double x, double y)
        {
            foreach(Rect area in selectedAreas)
            {
                if (area.Contains(x, y)) return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the uM concentration of ligand l to value val inside the Rect area, which represents an area in micrometers. 
        /// </summary>
        public void SetConcentrationInSelectedAreas(Ligand l, double val)
        {
            Debug.Print(String.Format("Setting concentration of {0} to {1:0.00}", l.Name, val));
            double step = 1.0 / settings.DX;

            foreach (Rect area in selectedAreas)
            {
                for (int i = (int)(area.X * step); i < (int)(step * (area.X + area.Width)); i++)
                {
                    for (int j = (int)(area.Y * step); j < (int)(step * (area.Y + area.Height)); j++)
                    {
                        c[l][width * j + i] = val;
                        c_m1[l][width * j + i] = val;
                    }
                }
            }
        }
        /// <summary>
        /// Gets the mean uM concentration of ligand l inside the Rect area (micrometer units). 
        /// </summary>
        public double GetConcentration(Ligand l, Rect area)
        {
            double step = 1.0 / settings.DX;

            double val = 0;
            int nSamples = 0;

            for (int i = (int)(area.X * step); i < (int)(step * (area.X + area.Width)); i++)
            {
                for (int j = (int)(area.Y * step); j < (int)(step * (area.Y + area.Height)); j++)
                {
                    nSamples++;
                    val+=c[l][width * j + i];
                }
            }

            return (val / nSamples);
        }

        /// <summary>
        /// Sets the concentration ligand l at point (not micron) position x,y to val uM. 
        /// </summary>
        public void SetConcentration(int x, int y, Ligand l, double val)
        {
            val = val < 0 ? 0 : val;
            int psn = y * width + x;
            if (psn < 0 || psn >= width * height) return;
            c[l][psn] = c_p1[l][psn] = c_m1[l][psn] = val;
        }


        /// <summary>
        /// Returns a true value if the point position x,y is open for diffusion, and returns false otherwise. 
        /// </summary>
        public bool IsOpen(int x, int y)
        {
            if (x < 0 || x >= Width || y<0 || y>= Height) return false;
            return (pointTypes[y*Width+x] & (byte)(Environment.PointType.FREE)) != 0;
        }
        /// <summary>
        /// Returns a true value if the micrometer position x,y is open for diffusion, and returns false otherwise. 
        /// </summary>
        public bool IsOpen(double dx, double dy)
        {
            int x = (int)Math.Floor(dx / settings.DX);
            int y = (int)Math.Floor(dy / settings.DX);

            return IsOpen(x, y);
        }

        /// <summary>
        /// Returns a true value if the point position x,y blocks cell movement and returns false otherwise. 
        /// </summary>
        public bool Blocked(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return true;
            return (pointTypes[y * Width + x] & (byte)(Environment.PointType.BLOCK)) != 0;
        }

        public bool Occupied(double x, double y)
        {
            int i = (int)Math.Floor(x / settings.DX);
            int j = (int)Math.Floor(y / settings.DX);
            return Occupied(i, j);
        }
        public bool Occupied(int x, int y)
        {
            int psn = y * width + x;
            if (psn < 0 || psn >= width * height) return true;
            else return occupiedSites[psn] > 0;
        }

        /// <summary>
        /// Returns a true value if the micrometer position x,y blocks cell movement and returns false otherwise. 
        /// </summary>
        public bool Blocked(double dx, double dy)
        {
            int x = (int)Math.Floor(dx / settings.DX);
            int y = (int)Math.Floor(dy / settings.DX);

            return Blocked(x, y);
        }

        public void OccupyPoints(IEnumerable<Point> points)
        {
            lock (occupiedSites) { 
            foreach (Point p in points)
            {
                int i = (int)Math.Floor(p.X / settings.DX);
                int j = (int)Math.Floor(p.Y / settings.DX);

                occupiedSites[j * width + i] += 1;
            }
            }
        }
        public void ReleasePoints(IEnumerable<Point> points)
        {
            lock (occupiedSites)
            {
                foreach (Point p in points)
                {
                    int i = (int)Math.Floor(p.X / settings.DX);
                    int j = (int)Math.Floor(p.Y / settings.DX);

                    occupiedSites[j * width + i] = occupiedSites[j * width + i] > 0 ? occupiedSites[j * width + i] - 1 : 0;
                }
            }
        }


        // Replace with interpolated version! DONE!
        /// <summary>
        /// Returns the concentration of ligand l at micrometer position x,y. 
        /// </summary>
        /// 
        public double GetConcentration(Ligand l, double x, double y)
        {
            double dx = settings.DX;
            
            int iMax = (int)Math.Ceiling(x / dx);
            int jMax = (int)Math.Ceiling(y / dx);

            int iMin = (int)Math.Floor(x / dx);
            int jMin = (int)Math.Floor(y / dx);

            return GetConcentration(l, iMin, jMin);

            if (iMax >= width) iMax = width - 1;
            if (jMax >= height) jMax = height - 1;
            if (iMin < 0) iMin = 0;
            if (jMin < 0) jMin = 0;

            double xMax = iMax * dx;
            double yMax = jMax * dx;
            double xMin = iMin * dx;
            double yMin = jMin * dx;

            double c00 = Math.Max(0, GetConcentration(l, iMin, jMin));
            double c01 = Math.Max(0, GetConcentration(l, iMin, jMax));
            double c10 = Math.Max(0, GetConcentration(l, iMax, jMin));
            double c11 = Math.Max(0, GetConcentration(l, iMax, jMax));

            if (xMax == xMin)
            {
                if (yMax == yMin) return c00;
                else
                {
                    return (1d / dx) * (c00 * (yMax - y) + c01 * (y - yMin));
                }
            }
            else if (yMax == yMin)
            {
                return (1d / dx) * (c00 * (xMax - x) + c10 * (x - xMin));
            }
            else
            {
                //if (xMax - x < 0.1 * dx) x = xMax; 
                //if (x - xMin < 0.1 * dx) x = xMin;
                //if (yMax - y < 0.1 * dx) y = yMax;
                //if (y - yMin < 0.1 * dx) y = yMin;

                double w00 = (1d / (dx * dx)) * (xMax - x) * (yMax - y);
                double w01 = (1d / (dx * dx)) * (xMax - x) * (y - yMin);
                double w10 = (1d / (dx * dx)) * (x - xMin) * (yMax - y);
                double w11 = (1d / (dx * dx)) * (x - xMin) * (y - yMin);


                double c_interp =  (
                            c00 * w00
                            + c01 * w01
                            + c10 * w10
                            + c11 * w11);
                /*
                if(
                    dx != (xMax - xMin)
                 || dx != (yMax - yMin)
                 || c_interp < 0
                 || c_interp > 10
                 || w00 < 0 || w00 > 1
                 || w01 < 0 || w01 > 1
                 || w10 < 0 || w10 > 1
                 || w11 < 0 || w11 > 1
                ){
                    Debug.WriteLine(string.Format("(x,y)::({3:0.0},{4:0.0}), w:: ({11:0.00},{12:0.00},{13:0.00},{14:0.00})     (jMin, jMax)::({5},{6})       dx: {0}, xMax-xMin: {1}, yMax-yMin: {2}", dx, (xMax - xMin), (yMax - yMin), x, y, jMin, jMax, c00, c01, c10, c11, w00, w01, w10, w11));
                }*/

                //if (Simulation.Current.Time > 45)
                //{
                //    Debug.WriteLine(string.Format("c_i:: {0:0.00},    c00:: {1:0.00}", c_interp, c00));
                //}

                return c00;
            }
            
        }
        public double GetConcentrationOld(Ligand l, double x, double y)
        {
            int i = (int)Math.Round(x/settings.DX);
            int j = (int)Math.Round(y/settings.DX);
            
            int psn = j * width + i;
            if (psn < 0 || psn >= width * height) return 0;

            return c[l][psn];
        }
        /// <summary>
        /// Returns the concentration of ligand l at point position x,y. 
        /// </summary>
        public double GetConcentration(Ligand l, int x, int y)
        { 
            int psn = y * width + x;
            if (psn < 0 || psn >= width * height) return 0;

            return c[l][psn];
        }

        public void PushReaction(int x, int y, double rate, int order, Ligand target, Ligand source = null)
        {
            if (source == null) source = target;

            int il_source = ligand_order[source];
            int il_target = ligand_order[target];
            int iPos = (y * width + x);

            int m_size = num_ligands * (1 + num_ligands);

            int xy_address = (iPos * m_size);
            int target_ligand_address = (1 + num_ligands) * il_target;
            //Possibly a bit silly, I doubt I will do second order without mixing types  // +1 here skipping zero order
            int source_ligand_order_address = order == 0 ? 0 : num_ligands * (order - 1) + il_source + 1;

                          // each position a matrix,        each ligand a row
            reactions_new[xy_address + target_ligand_address + source_ligand_order_address] += rate;
        }

        /// <summary>
        /// Relays mouse click to the internal regions. Not really for further use, but if you absolutely 
        /// have to emulate clicking somewhere you could use this.
        /// </summary>
        public void SendClick(Point position, MouseButtonEventArgs e)
        {
            Vector2Int intPsn = new Vector2Int((int)Math.Round(position.X / settings.DX), (int)Math.Round(position.Y / settings.DX));
            //System.Diagnostics.Debug.Print(string.Format("Environment click at int position ({0},{1})", intPsn.X, intPsn.Y));
            foreach(Region r in regions)
            {
                if (r.Contains(intPsn))
                {
                    //System.Diagnostics.Debug.Print(string.Format("Region contains click!"));
                    r.OnClick(e, this);
                    break;
                }
            }
        }

        /// <summary>
        /// Initialises the environment for use in the simulation. Parses the environment image, discovers regions,
        /// adds grids for each ligand.
        /// </summary>
        public void Init(Simulation sim)
        {
            //System.Diagnostics.Debug.WriteLine(String.Format("Init() environment"));
            RegionFinder parser = new RegionFinder();
            int w, h;
            foreach(Region r in parser.FindRegionsInImage(settings.ImagePath, out this.pointTypes, out w, out h))
            {
                regions.Add(r);
            }
            
            this.width = w;
            this.height = h;
            occupiedSites = new int[width * height]; 
            this.pre_k = 2d / (settings.DX * settings.DX);

            this.context = Context.Create(builder => builder.AllAccelerators());

            bool selected = false;

            if (context.GetCudaDevices().Count > 0 && !forceCPU)
            {
                accelerator = context.GetCudaDevice(0).CreateAccelerator(context);
                Trace.WriteLine("CUDA device chosen");
                accelerator.PrintInformation();
                selected = true;
            }
            if (!selected && context.GetCLDevices().Count > 0 && !forceCPU)
            {
                if (context.GetCLDevice(0).Extensions.Contains("cl_khr_fp64"))
                {
                    accelerator = context.GetCLDevice(0).CreateAccelerator(context);
                    Trace.WriteLine("CL device chosen");
                    accelerator.PrintInformation();
                    selected = true;
                }
                else
                {
                    Trace.WriteLine("CL device does not support fp64 (i.e. double precision!)");
                }
            }
            if(!selected)
            {
                accelerator = context.GetCPUDevice(0).CreateAccelerator(context);
                Trace.WriteLine("CPU device chosen");
                accelerator.PrintInformation();
            }
            

            int size = this.pointTypes.Length;
            num_ligands = 0;
            //System.Diagnostics.Debug.Print(string.Format("size = {0}", size));

            foreach (Ligand ligand in Model.Model.MasterElementList.OfType<Ligand>())
            {
                reactions.Add(ligand, new double[size]);
                c_m1.Add(ligand, new double[size]);
                c.Add(ligand, new double[size]);
                c_p1.Add(ligand, new double[size]);
                ligand_order.Add(ligand, num_ligands++);

            }
            //Is already the unwrapped reaction matrix. 
            reactions_new = new double[w * h * (1 + num_ligands) * num_ligands];

            if (num_ligands == 0) return;

            kernel_rx2 = accelerator.Allocate1D<double>(new double[w * h * ((1+num_ligands) * num_ligands)]);
            kernel_rxn  = accelerator.Allocate1D<double>(new double[w * h * num_ligands]);
            kernel_cm1  = accelerator.Allocate1D<double>(new double[w * h * num_ligands]);
            kernel_c    = accelerator.Allocate1D<double>(new double[w * h * num_ligands]);
            kernel_cp1  = accelerator.Allocate1D<double>(new double[w * h * num_ligands]);
            kernel_dif  = accelerator.Allocate1D<double>(new double[num_ligands]);
            kernel_mask = accelerator.Allocate1D<byte>(new byte[w * h]);

            foreach(Region region in regions)
            {
                region.Init(sim, this);
            }

            loadedKernel = accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<double>, ArrayView<double>, ArrayView<double>, ArrayView<byte>, ArrayView<double>, int, ArrayView<double>, double, int, int>(DuFortKernel);
            reactionKernel = accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<double>, ArrayView<double>, ArrayView<double>, ArrayView<byte>, ArrayView<double>, int, double, int, int>(ReactionKernel);
            swapKernel = accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<double>, ArrayView<double>, ArrayView<double>>(SwapKernel);

            rxn_full = new double[num_ligands * width * height];
            cm1_full = new double[num_ligands * width * height];
            c00_full = new double[num_ligands * width * height];
            cp1_full = new double[num_ligands * width * height];
        }

        /// <summary>
        /// Deals with diffusive update to ligand concentrations. Called as part of simulation.Update. Uses ILGPU to supply a parallel acceleration of calculation.
        /// </summary>
        public void Update(double dt)
        {
            double k;
            double ks;
            double dts;
            int substeps;
            int nLigands = c.Keys.Count;

            if (nLigands == 0) return;

            kernel_mask.CopyFromCPU(pointTypes);

            Array.Clear(rxn_full);
            Array.Clear(cm1_full); 
            Array.Clear(c00_full);
            Array.Clear(cp1_full);

            double diffMax = 0;

            double[] diffs = new double[nLigands];
            foreach (Ligand l in c.Keys)
            {
                int iL = ligand_order[l]; 
                diffs[iL] = l.Diffusivity;
                if (l.Diffusivity> diffMax)
                {
                    diffMax = l.Diffusivity;
                }
                
                Array.Copy(reactions[l], 0, rxn_full, width * height * iL, width * height);
                Array.Copy(c_m1[l], 0, cm1_full, width * height * iL, width * height);
                Array.Copy(c[l], 0, c00_full, width * height * iL, width * height);
            }

            kernel_rx2.CopyFromCPU(reactions_new);
            kernel_rxn.CopyFromCPU(rxn_full);
            kernel_cm1.CopyFromCPU(cm1_full);
            kernel_c.CopyFromCPU(c00_full);

            k = pre_k * dt * diffMax;
            //Subdivide for closest to "consistent" k=1 value
            substeps = 5*(int)Math.Max(1, Math.Round(0.5 * k));
            ks = (k / (1d * substeps));
            dts = (dt / (1d * substeps));

            int rx_stride = (1 + num_ligands) * num_ligands;

            
            for (int i=0; i<diffs.Length; i++)
            {
                diffs[i] *= ks / diffMax;
            }
            kernel_dif.CopyFromCPU(diffs);

            //System.Diagnostics.Debug.Print(string.Format("{0} steps for {1}", substeps, l.Name));

            for (int idx=0; idx<substeps; idx++)
            {
                loadedKernel((int)kernel_cp1.Length, kernel_cm1.View, kernel_c.View, kernel_cp1.View, kernel_mask.View, kernel_rx2.View, num_ligands, kernel_dif.View, dts, this.width, this.height);
                reactionKernel((int)kernel_cp1.Length, kernel_cm1.View, kernel_c.View, kernel_cp1.View, kernel_mask.View, kernel_rx2.View, num_ligands, dts, this.width, this.height);
                swapKernel((int) kernel_cp1.Length,   kernel_cm1.View, kernel_c.View, kernel_cp1.View);
            }
                
            //accelerator.Synchronize();
            //kernel_cp1.CopyTo(c_p1[l], 0, 0, c_p1[l].Length);
            c00_full = kernel_c.GetAsArray1D();
            cm1_full = kernel_cm1.GetAsArray1D();
            
            double baserate;
            Array.Clear(reactions_new);
            //Value update the past concentrations.
            foreach (Ligand l in c.Keys)
            {
                int iL = ligand_order[l];
                Array.Copy(c00_full, width * height * iL, c[l],    0, width * height);
                Array.Copy(cm1_full, width * height * iL, c_m1[l], 0, width * height);

                for (int i = 0; i < reactions[l].Length; i++)
                {
                    if (((pointTypes[i] & (byte)(Environment.PointType.FREE)) != 0))
                    {
                        baserate = l.FeedRate - c[l][i] * l.KillRate;
                        reactions[l][i] = baserate;

                        int x = i % width;
                        int y = i / width;

                        PushReaction(x: x, y: y, rate:  l.FeedRate, order: 0, target: l);
                        PushReaction(x: x, y: y, rate: -l.KillRate, order: 1, target: l);
                    }
                    else reactions[l][i] = 0;
                }
            }
        }

        
        //This should be replaced with something in the kernel that can dynamically include losses introuced here.
        /// <summary>
        /// TEMP SOLUTION ONLY. Method for ligand concentration alteration, but directly alters the grid, rather than being included in the
        /// update kernel which would be preferable. 
        /// </summary>
        /// <param name="input">Ligand being degraded</param>
        /// <param name="output">Ligand being produced</param>
        /// <param name="x">x position in microns</param>
        /// <param name="y">y position in microns</param>
        /// <param name="rate">rate of change of concentration</param>
        /// <param name="dt">Timestep (mins)</param>
        
        public void DegradeAtRate(Ligand input, Ligand output, double x, double y, double rate0, double rate1, double io_ratio, double dt)
        {
            int ix = (int)(x / settings.DX);
            int iy = (int)(y / settings.DX);


            //int pos =  + Width * (int)Math.Floor(y / settings.DX);

            if (input != null)
            {
                PushReaction(ix, iy, -rate0, 0, input);
                PushReaction(ix, iy, -rate1, 1, input); 
               

                //reactions[input][pos] -= rate;
            }
            if (output != null)
            {
                PushReaction(ix, iy, rate0, 0, output);
                if(input!=null) PushReaction(ix, iy, rate1, 1, output, input);
                //reactions[output][pos] += io_ratio*rate;
            }
        }

        public byte GetPointRules(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return 0;
            return this.pointTypes[y*Width+x];
        }

        //This is more honestly my level. CUDA melts my brain. EDIT- Leaving in temporarily, but the new kernel seems to work better for all applications. 
        [Obsolete("DuFortKernel_CPU is deprecated, please use DuFortKernel, which works on any hardware.")]
        private void DuFortKernel_CPU(double[] cm1, double[] c, double[] cp1, double[] ux, double[] uy, double[] fk, byte[] mask, double dt, double k, int n, int w, int h)
        {

            int i = n % w;
            int j = n / w;

            //                         N 
            // node (i,j)              |
            // node (i,j+1)            |
            // node (i,j-1)     W ---- C ---- E
            // node (i+1,j)            |
            // node (i-1,j)            |
            //                         S 


            int C = i + j * w;
            int N = i + (j + 1) * w;
            int S = i + (j - 1) * w;
            int E = (i + 1) + j * w;
            int W = (i - 1) + j * w;

            //System.Diagnostics.Debug.Print(string.Format("[{0}::{1}]->({2},{3})", n,C, i, j));

            // Bounds checking. 
            N = j == h-1 ? C : N;
            S = j == 0   ? C : S;
            E = i == w-1 ? C : E;
            W = i == 0   ? C : W;

            if ((mask[C] & (byte) Environment.PointType.FIXED)!=0)
            {
                //CODE HERE TO SKIP, AS WE DON'T UPDATE FIXED POINTS.
                //System.Diagnostics.Debug.Print(string.Format("({1},{2}) is fixed, skipping", n, i, j));
                return;
            }

            // If any neighbours are not free points, use the centre instead to enforce Neumann conditions. 
            N = ((mask[N] & (byte) (Environment.PointType.FREE)) != 0) ? N : C;
            S = ((mask[S] & (byte) (Environment.PointType.FREE)) != 0) ? S : C;
            E = ((mask[E] & (byte) (Environment.PointType.FREE)) != 0) ? E : C;
            W = ((mask[W] & (byte) (Environment.PointType.FREE)) != 0) ? W : C;

            if (c[C] > 0)
            {
                //System.Diagnostics.Debug.Print(string.Format("coords: (i,j)::({6},{7}), C::{0} N::{1} S::{2} E::{3} W::{4}, k::{5}", C, N, S, E, W,k,i,j));
                //System.Diagnostics.Debug.Print(string.Format("conc:   C::{0:0.0} N::{1:0.0} S::{2:0.0} E::{3:0.0} W::{4:0.0}", cm1[C], c[N], c[S], c[E], c[W]));
            }

            cp1[C] = ((1d - 2d * k) / (1d + 2d * k)) * cm1[C] + (k / (1d + 2d * k)) * (c[N] + c[S] + c[E] + c[W]);
        }

        private static void DuFortKernel(Index1D n, ArrayView<double> cm1, ArrayView<double> c, ArrayView<double> cp1, ArrayView<byte> mask, ArrayView<double> react, int num_ligands, ArrayView<double> k, double dt, int w, int h)
        {

            int i = n % w;
            int j = (n / w) % h;
            int l = n / (w * h);

            //                         N 
            // node (i,j)              |
            // node (i,j+1)            |
            // node (i,j-1)     W ---- C ---- E
            // node (i+1,j)            |
            // node (i-1,j)            |
            //                         S 


            int C = i + j * w;
            int N = i + (j + 1) * w;
            int S = i + (j - 1) * w;
            int E = (i + 1) + j * w;
            int W = (i - 1) + j * w;
            int L = l * w * h;

            //System.Diagnostics.Debug.Print(string.Format("[{0}::{1}]->({2},{3})", n,C, i, j));

            // Bounds checking. 
            N = j == h - 1 ? C : N;
            S = j == 0 ? C : S;
            E = i == w - 1 ? C : E;
            W = i == 0 ? C : W;

            if ((mask[C] & (byte)Environment.PointType.FIXED) != 0)
            {
                //CODE HERE TO SKIP, AS WE DON'T UPDATE FIXED POINTS.
                //System.Diagnostics.Debug.Print(string.Format("({1},{2}) is fixed, skipping", n, i, j));
                cp1[C + L] = c[C + L];
                return;
            }
            byte fxd = (byte)(Environment.PointType.FREE);
            // If any neighbours are not free points, use the centre instead to enforce Neumann conditions. 
            N = ((mask[N] & fxd) != 0) ? N : C;
            S = ((mask[S] & fxd) != 0) ? S : C;
            E = ((mask[E] & fxd) != 0) ? E : C;
            W = ((mask[W] & fxd) != 0) ? W : C;

            N = N + L;
            S = S + L;
            E = E + L;
            W = W + L;
            C = C + L;

            cp1[C] = ((1d - 2d * k[l]) / (1d + 2d * k[l])) * cm1[C] + (k[l] / (1d + 2d * k[l])) * (c[N] + c[S] + c[E] + c[W]);
            //cp1[C] = c[C];
        }

        private static void ReactionKernel(Index1D n, ArrayView<double> cm1, ArrayView<double> c, ArrayView<double> cp1, ArrayView<byte> mask, ArrayView<double> react, int num_ligands, double dt, int w, int h)
        {
            int i = n % w;
            int j = (n / w) % h;
            int l = n / (w * h);

            int C = i + j * w;
            int N = i + (j + 1) * w;
            int S = i + (j - 1) * w;
            int E = (i + 1) + j * w;
            int W = (i - 1) + j * w;
            int L = l * w * h;

            if ((mask[C] & (byte)Environment.PointType.FIXED) != 0)
            {
                cp1[C + L] = c[C + L];
                if (cp1[C] < 0) cp1[C] = 0;
                return;
            }

            // Bounds checking. 
            N = j == h - 1 ? C : N;
            S = j == 0 ? C : S;
            E = i == w - 1 ? C : E;
            W = i == 0 ? C : W;
            
            byte fxd = (byte)(Environment.PointType.FREE);
            // If any neighbours are not free points, use the centre instead to enforce Neumann conditions. 
            N = ((mask[N] & fxd) != 0) ? N : C;
            S = ((mask[S] & fxd) != 0) ? S : C;
            E = ((mask[E] & fxd) != 0) ? E : C;
            W = ((mask[W] & fxd) != 0) ? W : C;

            int rxn_stride = (1 + num_ligands) * num_ligands;

            //Centre*matrix size+ ligand num times row size, zero order.
            double fk0 = dt * react[C * rxn_stride + l * (1 + num_ligands)];// / (1 + 2 * k);
            double fk1 = 0;

            //Iterate all other ligand effects
            for (int lig_in = 0; lig_in < num_ligands; lig_in++)
            {
                fk1 += c[C + (lig_in * w * h)] * react[C * rxn_stride + l * (1 + num_ligands) + lig_in + 1] * dt;// /(1 + 2 * k);
            }

            C = C + L;
            N = N + L;
            S = S + L;
            E = E + L;
            W = W + L;
            double fk = fk0 + fk1;

            c[C] += 0.177 * fk;
            c[N] += 0.111 * fk;
            c[S] += 0.111 * fk;
            c[E] += 0.111 * fk;
            c[W] += 0.111 * fk;

            cp1[C] += 0.292 * fk;
            cp1[N] += 0.177 * fk;
            cp1[S] += 0.177 * fk;
            cp1[E] += 0.177 * fk;
            cp1[W] += 0.177 * fk;

            if (cp1[C] < 0) cp1[C] = 0;
            
        }

        private static void SwapKernel(Index1D n, ArrayView<double> cm1, ArrayView<double> c, ArrayView<double> cp1)
        {
            cm1[n] = c[n];
            c[n] = cp1[n];
        }


        public void DrawRegions(WriteableBitmap bmp)
        {
            using (bmp.GetBitmapContext())
            {
                foreach(Region r in this.regions)
                {
                    r.Draw(bmp, this);
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.c.Keys.Count > 0)
                    {
                        kernel_cm1.Dispose();
                        kernel_c.Dispose();
                        kernel_cp1.Dispose();
                        kernel_mask.Dispose();
                        accelerator.Dispose();
                        context.Dispose();
                    }
                }

                this.c.Clear();
                this.c_m1.Clear();
                this.c_p1.Clear();
                this.regions.Clear();

                this.pointTypes = null;

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Environment()
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
