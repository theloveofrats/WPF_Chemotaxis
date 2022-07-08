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
        protected MemoryBuffer1D<double, Stride1D.Dense> kernel_cm1;
        protected MemoryBuffer1D<double, Stride1D.Dense> kernel_c;
        protected MemoryBuffer1D<double, Stride1D.Dense> kernel_cp1;
        protected MemoryBuffer1D<double, Stride1D.Dense> kernel_tmp;
        protected MemoryBuffer1D<byte, Stride1D.Dense> kernel_mask;


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

        private byte[] pointTypes;

        private Dictionary<Ligand, double[]> reactions = new();
        private Dictionary<Ligand, double[]> c_m1 = new();
        private Dictionary<Ligand, double[]> c_p1 = new();
        private Dictionary<Ligand, double[]> c    = new();

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
        /// <summary>
        /// Gets the value of the flag for PointType type at point position (not micrometer position) x,y. 
        /// </summary>
        public bool GetFlag(int x, int y, PointType type)
        {
            int psn = width * y + x;
            byte pointType = (byte)type;

            return (this.pointTypes[psn] & pointType) == pointType;
        }

        /// <summary>
        /// Sets the uM concentration of ligand l to value val inside the Rect area, which represents an area in micrometers. 
        /// </summary>
        public void SetConcentration(Ligand l, Rect area, double val)
        {
            double step = 1.0 / settings.DX;

            for(int i=(int)(area.X*step); i<(int)(step*(area.X+area.Width)); i++)
            {
                for (int j = (int)(area.Y * step); j < (int)(step * (area.Y + area.Height)); j++)
                {
                    c[l][width * j + i] = val;
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

        /// <summary>
        /// Returns a true value if the micrometer position x,y blocks cell movement and returns false otherwise. 
        /// </summary>
        public bool Blocked(double dx, double dy)
        {
            int x = (int)Math.Floor(dx / settings.DX);
            int y = (int)Math.Floor(dy / settings.DX);

            return Blocked(x, y);
        }

        // Replace with interpolated version! DONE!
        /// <summary>
        /// Returns the concentration of ligand l at micrometer position x,y. 
        /// </summary>
        /// 
        public double GetConcentration(Ligand l, double x, double y)
        {
            int iMax = (int)Math.Ceiling(x / settings.DX);
            int jMax = (int)Math.Ceiling(y / settings.DX);

            int iMin = (int)Math.Floor(x / settings.DX);
            int jMin = (int)Math.Floor(y / settings.DX);

            double xMax = iMax * settings.DX;
            double yMax = jMax * settings.DX;
            double xMin = iMin * settings.DX;
            double yMin = jMin * settings.DX;

            if (xMax >= width) xMax = width - 1;
            if (yMax >= height) yMax = height - 1;
            if (xMin < 0) xMin = 0;
            if (yMin < 0) yMin = 0;

            double c00 = GetConcentration(l, iMin, jMin);
            double c01 = GetConcentration(l, iMin, jMax);
            double c10 = GetConcentration(l, iMax, jMin);
            double c11 = GetConcentration(l, iMax, jMax);

            double c_interp = (1d / ((xMax - xMin) * (yMax - yMin))) * (
                        c00 * (xMax - x) * (yMax - y)
                        + c01 * (xMax - x) * (y - yMin)
                        + c10 * (x - xMin) * (yMax - y)
                        + c11 * (x - xMin) * (y - yMin));
            
            return c_interp;
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

            RegionFinder parser = new RegionFinder();
            int w, h;
            foreach(Region r in parser.FindRegionsInImage(settings.ImagePath, out this.pointTypes, out w, out h))
            {
                regions.Add(r);
            }
            
            this.width = w;
            this.height = h;
            this.pre_k = 2d / (settings.DX * settings.DX);

            this.context = Context.Create(builder => builder.AllAccelerators());
            if (context.GetCudaDevices().Count > 0 && !forceCPU)
            {
                accelerator = context.GetCudaDevice(0).CreateAccelerator(context);
                System.Diagnostics.Debug.Print("CUDA device chosen");
                accelerator.PrintInformation();
            }
            else if (context.GetCLDevices().Count > 0 && !forceCPU)
            {
                accelerator = context.GetCLDevice(0).CreateAccelerator(context);
                System.Diagnostics.Debug.Print("CL device chosen");
                accelerator.PrintInformation();
            }
            else
            {
                accelerator = context.GetCPUDevice(0).CreateAccelerator(context);
                System.Diagnostics.Debug.Print("CPU device chosen");
                accelerator.PrintInformation();
            }
            

            int size = this.pointTypes.Length;
            int num_ligands = 0;
            //System.Diagnostics.Debug.Print(string.Format("size = {0}", size));
            foreach(Ligand ligand in Model.Model.MasterElementList.OfType<Ligand>())
            {
                reactions.Add(ligand, new double[size]);
                c_m1.Add(ligand, new double[size]);
                c.Add(ligand, new double[size]);
                c_p1.Add(ligand, new double[size]);
                num_ligands++;
            }
            kernel_rxn = accelerator.Allocate1D<double>(new double[w * h]);
            kernel_cm1  = accelerator.Allocate1D<double>(new double[w * h]);
            kernel_c    = accelerator.Allocate1D<double>(new double[w * h]);
            kernel_cp1  = accelerator.Allocate1D<double>(new double[w * h]);
            //kernel_tmp = accelerator.Allocate<double>(new double[w * h]);
            kernel_mask = accelerator.Allocate1D<byte>(new byte[w * h]);

            foreach(Region region in regions)
            {
                region.Init(sim, this);
            }
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

            Action<Index1D, ArrayView<double>, ArrayView<double>, ArrayView<double>, ArrayView<byte>, ArrayView<double>, double, double, int, int> loadedKernel = accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<double>, ArrayView<double>, ArrayView<double>, ArrayView<byte>, ArrayView<double>, double, double, int, int>(DuFortKernel);
            Action<Index1D, ArrayView<double>, ArrayView<double>, ArrayView<double>> swapKernel = accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<double>, ArrayView<double>, ArrayView<double>>(SwapKernel);


            foreach (Ligand l in c.Keys)
            {
                k = pre_k * dt * l.Diffusivity;

                //Subdivide for closest to "consistent" k=1 value
                substeps = (int) Math.Max(1, Math.Round(0.5*k));
                ks = (k / (1d * substeps));
                dts = (dt / (1d * substeps));

                kernel_rxn.CopyFromCPU(reactions[l]);
                kernel_cm1.CopyFromCPU(c_m1[l]);
                kernel_c.CopyFromCPU(c[l]);
                kernel_mask.CopyFromCPU(pointTypes);

                //System.Diagnostics.Debug.Print(string.Format("{0} steps for {1}", substeps, l.Name));

                for (int idx=0; idx<substeps; idx++)
                {
                    loadedKernel((int) kernel_cp1.Length, kernel_cm1.View, kernel_c.View, kernel_cp1.View, kernel_mask.View, kernel_rxn.View, ks, dts, this.width, this.height);
                    swapKernel((int) kernel_cp1.Length,   kernel_cm1.View, kernel_c.View, kernel_cp1.View);
                }
                
                //accelerator.Synchronize();
                //kernel_cp1.CopyTo(c_p1[l], 0, 0, c_p1[l].Length);

                c[l]    = kernel_c.GetAsArray1D();
                c_m1[l] = kernel_cm1.GetAsArray1D();
            }

            
            double[] temp;
            double baserate;
            //Value update the past concentrations.
            foreach (Ligand l in c.Keys)
            {
                for (int i = 0; i < reactions[l].Length; i++)
                {
                    if (((pointTypes[i] & (byte)(Environment.PointType.FREE)) != 0))
                    {
                        baserate = l.FeedRate - c[l][i] * l.KillRate;

                        reactions[l][i] = baserate;
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
        public void DegradeAtRate(Ligand input, Ligand output, double x, double y, double rate, double io_ratio, double dt)
        {
            int pos = (int)Math.Floor(x / settings.DX) + Width * (int)Math.Floor(y / settings.DX);

            if (input != null)
            {
                reactions[input][pos] -= rate;
            }
            if (output != null)
            {
                reactions[output][pos] += io_ratio*rate;
            }
        }
        

        /*
        [Kernel]
        // cm1, c, cp1 are the DuFort Frankel concentrations for t-1,t,t+1. ux,uy are the advective terms. fk is the feed term. mask is a bitmask that contains the state of the square. dt is the time delta, k is the diffusion constant/dx^2
        // NOTE I HAVE SO FAR ONLY IMPLEMENTED DIFFUSION.
        
        private void DuFortKernel_GPU(deviceptr<double[]> cm1, deviceptr<double[]> c, deviceptr<double[]> cp1, deviceptr<double[]> ux, deviceptr<double[]> uy, deviceptr<double[]> fk, deviceptr<byte[]> mask, double dt, double k)
        {

            const int i = blockIdx.x * blockDim.x + threadIdx.x;
            const int j = blockIdx.y * blockDim.y + threadIdx.y;

            //                         N 
            // node (i,j)              |
            // node (i,j+1)            |
            // node (i,j-1)     W ---- C ---- E
            // node (i+1,j)            |
            // node (i-1,j)            |
            //                         S 


            int C = i + j * NX;                         
            int N = i + (j + 1) * NX;                   
            int S = i + (j - 1) * NX;                   
            int E = (i + 1) + j * NX;                   
            int W = (i - 1) + j * NX;

            // Bounds checking (Neumann). 
            N = N >= h ? C : N;
            S = S <  0 ? C : S;
            E = E >= w ? C : E;
            W = W <  0 ? C : W;

            if (mask[C] & Environment.PointType.FIXED)
            {
                //CODE HERE TO SKIP, AS WE DON'T UPDATE FIXED POINTS.

            }
            // If any neighbours are not free, use the centre instead to enforce Neumann conditions. 
            N = (mask[N] & Environment.PointType.FREE) ? N : C;
            S = (mask[S] & Environment.PointType.FREE) ? S : C;
            E = (mask[E] & Environment.PointType.FREE) ? E : C;
            W = (mask[W] & Environment.PointType.FREE) ? W : C;

            cp1[C] = ((1d - 2d * k) / (1d + 2d * k)) * cm1[C]   +   (k/(1d+2d*k)) * (c[N]*c[S]+c[E]+c[W]);
        }*/


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

        private static void DuFortKernel(Index1D n, ArrayView<double> cm1, ArrayView<double> c, ArrayView<double> cp1, ArrayView<byte> mask, ArrayView<double> react, double k, double dt, int w, int h)
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
            N = j == h - 1 ? C : N;
            S = j == 0 ? C : S;
            E = i == w - 1 ? C : E;
            W = i == 0 ? C : W;

            if ((mask[C] & (byte)Environment.PointType.FIXED) != 0)
            {
                //CODE HERE TO SKIP, AS WE DON'T UPDATE FIXED POINTS.
                //System.Diagnostics.Debug.Print(string.Format("({1},{2}) is fixed, skipping", n, i, j));
                cp1[C] = c[C];
                return;
            }

            byte fxd = (byte)(Environment.PointType.FREE);
                // If any neighbours are not free points, use the centre instead to enforce Neumann conditions. 
            N = ((mask[N] & fxd) != 0) ? N : C;
            S = ((mask[S] & fxd) != 0) ? S : C;
            E = ((mask[E] & fxd) != 0) ? E : C;
            W = ((mask[W] & fxd) != 0) ? W : C;

            double fk = dt * react[C] / (1 + 2 * k);
            fk = fk <= c[C] ? fk : c[C];

            cp1[C] = ((1d - 2d * k) / (1d + 2d * k)) * cm1[C] + (k / (1d + 2d * k)) * (c[N] + c[S] + c[E] + c[W]) + fk;
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
                    kernel_cm1.Dispose();
                    kernel_c.Dispose();
                    kernel_cp1.Dispose();
                    kernel_mask.Dispose();
                    accelerator.Dispose();
                    context.Dispose();
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
