using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using WPF_Chemotaxis.UX;
using WPF_Chemotaxis.Model;
using WPF_Chemotaxis.Simulations;
using System.Threading;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Interop;
using System.Windows.Controls.Primitives;
using System.IO;

namespace WPF_Chemotaxis
{
    /// <summary>
    /// Interaction logic for DisplayWindow.xaml
    /// </summary>
    public partial class DisplayWindow : Window, INotifyPropertyChanged
    {
        private static int numDisplays = 0;
        private Simulation simulation;
        private WriteableBitmap bmp;
        private WriteableBitmap bmp_overlay;
        private DispatcherTimer timer = new DispatcherTimer();
        private IntensityInterpreter interpreter;
        private IHeatMapSource currentHeatMapSource { get; set; }
        private OverlaySelector selector;

        private List<HeatPoint> intensities = new List<HeatPoint>();
        private int frameCount = 0;
      
        public event PropertyChangedEventHandler PropertyChanged;
        BitmapEncoder pngEncoder = new PngBitmapEncoder();

        //private List<ILinkable> heatMapSources; 

        public DisplayWindow()
        {
            InitializeComponent();
            InitialiseHeatMapSources();
            cbColors.SelectedIndex = 0;
            this.interpreter = new IntensityInterpreter(this.cbColors);

            timer.Interval = new TimeSpan(1000000);
            timer.Tick += (d, e) => {
                RedrawImage();
                RedrawOverlay();
            };
        }

        public void Window_Closed(object sender, CancelEventArgs e)
        {
            if(simulation!=null) simulation.Cancel();
        }


        private void InitialiseHeatMapSources()
        {
            cbDisplaySources.ItemsSource = Model.Model.MasterElementList.Where(link => link.GetType().IsAssignableTo(typeof(IHeatMapSource))).ToList();
            cbDisplaySources.SelectedIndex = 0;
            ReloadHeatmapSourceOpts();
        }

        public void LinkSimulation(Simulation simulation, UniformGrid chartTarget)
        {
            this.simulation = simulation;
            this.InitialiseIntensities();
            this.simulation.Redraw += (s, e, c) => this.UpdateSourceData(e, c);
            this.simulation.Close += (s,e,c) => this.FinishSimulation();
            this.InitialiseImages();
            this.selector = new OverlaySelector(simulation);
            int displayNum = ++numDisplays;

            //This connects the target panel in the main window to the selector. It's ugly as hell like this, though.
            ChartManager manager = new ChartManager(chartTarget, selector);
            simulation.LateUpdate  += (s, e, m) => this.Dispatcher.Invoke(() => manager.DoChart());
            simulation.WriteToFile += (s, e, m) => this.Dispatcher.Invoke(() => this.SavePNGFile(s.TargetDirectory, displayNum));
        }

        private void FinishSimulation()
        {
            this.timer.Stop(); 
            this.simulation = null;
            Dispatcher.Invoke(()=> this.Close());
        }

        private void SavePNGFile(string baseDirectory, int displayNum)
        {
            string targetDir = baseDirectory + string.Format("\\Images\\Display {0}\\", displayNum);
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            Rect bounds = VisualTreeHelper.GetDescendantBounds(displayWindowImage);
            double dpi = 96d;

            RenderTargetBitmap rtb = new RenderTargetBitmap((int)bounds.Width, (int)bounds.Height, dpi, dpi, System.Windows.Media.PixelFormats.Default);

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(displayWindowImage);
                VisualBrush vb2 = new VisualBrush(overlayImage);
                dc.DrawRectangle(vb, null, new Rect(new Point(), bounds.Size));
                dc.DrawRectangle(vb2, null, new Rect(new Point(), bounds.Size));
            }

            rtb.Render(dv);

            /*WriteableBitmap merge = this.bmp.Resize(this.bmp_overlay.PixelWidth, this.bmp_overlay.PixelHeight, WriteableBitmapExtensions.Interpolation.Bilinear);
            Color overClr;
            for(int i=0; i<merge.PixelWidth; i++)
            {
                for(int j=0; j<merge.PixelHeight; j++)
                {
                    overClr = bmp_overlay.GetPixel(i, j);
                    if(overClr.A<=2) merge.SetPixel(i, j, overClr);
                }
            }*/

            string filename = targetDir + string.Format("Frame{0:000000}.png", frameCount++);
            using (FileStream stream = new FileStream(filename, FileMode.Create))
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));
                encoder.Save(stream);
            }
        }

        private void CheckOverlayImageSize()
        {
            var source = new HwndSource(new HwndSourceParameters());
            Matrix ttd = source.CompositionTarget.TransformToDevice;
            Point pxdims = ttd.Transform(new Point(overlayImage.ActualWidth, overlayImage.ActualHeight));
            if (pxdims.X > 1.1 * bmp_overlay.PixelWidth || pxdims.X<0.9* bmp_overlay.PixelWidth)
            {
                bmp_overlay = bmp_overlay.Resize((int)pxdims.X, (int)pxdims.Y, WriteableBitmapExtensions.Interpolation.Bilinear);
                overlayImage.Source = bmp_overlay;
            }
        }

        /// <summary>
        /// Run once as display window loads. Sets up image width and height, and then paints the static elements. Dynamic elements are painted later (and, of course, repeatedly!) 
        /// </summary>
        private void InitialiseImages()
        {
            this.bmp = BitmapFactory.New(simulation.W, simulation.H);
            this.displayWindowImage.Source = this.bmp;

            this.bmp_overlay = BitmapFactory.New(simulation.W, simulation.H);
            this.overlayImage.Source = bmp_overlay;

            this.bmp_overlay.Clear(Colors.Transparent);

            Color clr;
            for (int i=0; i<this.simulation.W; i++)
            {
                for(int j=0; j<this.simulation.H; j++)
                {
                    // Only not-free wall points
                    clr = Colors.White;
                    if (!simulation.Environment.IsOpen(i,j)) {
                        if((simulation.Environment.IsOpen(i+1,j)) || (simulation.Environment.IsOpen(i - 1, j)) || (simulation.Environment.IsOpen(i, j+1)) || (simulation.Environment.IsOpen(i, j-1)))
                        {
                            clr = Colors.Black;
                        }
                    }
                    WriteableBitmapExtensions.SetPixel(this.bmp, i, j, clr);
                }
            }

            
            
        }
        /// <summary>
        /// Starts the thread responsible for drawing.
        /// </summary>
        public void Start()
        {
            timer.Start();   
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CheckOverlayImageSize();
        }

        public void Redraw(object sender, RoutedEventArgs e)
        {
            RedrawImage();
            RedrawOverlay();
            CheckOverlayImageSize();
        }
        private void InitialiseIntensities()
        {
            for(int i=0; i<this.simulation.W; i++)
            {
                for(int j=0; j<this.simulation.H; j++)
                {
                    byte state = simulation.Environment.GetPointRules(i, j);
                    if((state & ((byte)Simulations.Environment.PointType.FREE)) != 0)
                    {
                        intensities.Add(new HeatPoint(i,j,0));
                    }
                }
            }
        }
        private void UpdateSourceData(Simulations.Environment environment, IEnumerable<Cell> cells)
        {
            FetchIntensities(environment);
        }

        // Not appropriate here, needs its own mediating class, but I need to have a working solution before I can tweak it...
        private void FetchIntensities(Simulations.Environment environment)
        {
            lock (this.intensities) {
                foreach (HeatPoint hp in this.intensities)
                {
                    hp.Intensity = this.currentHeatMapSource.GetIntensity(environment, hp.X, hp.Y);
                }
            }
        }

        // Redraw only references those pixels that refer to initially open, diffusible points in the maze.
        private void RedrawImage()
        {
            Color clr;
            lock (this.intensities)
            {
                using (bmp.GetBitmapContext())
                {
                    foreach (HeatPoint hp in this.intensities)
                    {
                        if ((simulation.Environment.GetPointRules(hp.X, hp.Y) & (byte)(Simulations.Environment.PointType.FREE)) != 0)
                        {
                            clr = this.interpreter.IntensityToColor(hp.Intensity);
                        }
                        WriteableBitmapExtensions.SetPixel(this.bmp, hp.X, hp.Y, clr);
                    }
                }
            }
            simulation.Environment.DrawRegions(bmp);
        }
        private void RedrawOverlay()
        { 
            bmp_overlay.Clear(Colors.Transparent);
            ICollection<Cell> cells = simulation.Cells;
            double dx = simulation.Environment.settings.DX;

            using (bmp_overlay.GetBitmapContext())
            {
                foreach (Cell c in cells)
                {
                    c.Draw(simulation.Environment, bmp_overlay);
                }
                selector.DrawSelection(this.simulation, bmp_overlay);
            }
        }

        private void OnHeatmapSourceSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            lock (this.intensities)
            {
                currentHeatMapSource = (IHeatMapSource)(cbDisplaySources.SelectedItem);
                ReloadHeatmapSourceOpts();
            }
        }
        private void OnHeatmapOptsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            lock (this.intensities)
            {
                double min, max;
                currentHeatMapSource.SetMethod(cbSourceOpts.SelectedIndex, out min, out max);
            }
        }

        private void ReloadHeatmapSourceOpts()
        {
            cbSourceOpts.ItemsSource = currentHeatMapSource.GetIntensityMethods();
            cbSourceOpts.SelectedIndex = 0;
        }

        private Point ScreenToSimulation(Image clicked, Point clickPoint)
        {

            double x = clickPoint.X * simulation.Environment.settings.DX * simulation.Environment.Width / clicked.ActualWidth;
            double y = clickPoint.Y * simulation.Environment.settings.DX * simulation.Environment.Width / clicked.ActualWidth;


            return new Point(x, y);
        }

        private void MouseDown_OverlayImage(object sender, MouseButtonEventArgs e)
        {
            //Translates mouse event position to simulation position 
            Image clicked = (Image)sender;
            Point psn = ScreenToSimulation(clicked, e.GetPosition(clicked));

           
            if (e.ChangedButton==MouseButton.Left) selector.OnLeftMouseDown(psn.X, psn.Y);
        }
        private void MouseDrag_OverlayImage(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                //Translates mouse event position to simulation position 
                Image clicked = (Image)sender;
                Point psn = ScreenToSimulation(clicked, e.GetPosition(clicked));
                selector.OnDrag(psn.X, psn.Y);
            }
        }
        private void MouseUp_OverlayImage(object sender, MouseButtonEventArgs e)
        {
            //Translates mouse event position to simulation position 


            Image clicked = (Image)sender;
            Point psn = ScreenToSimulation(clicked, e.GetPosition(clicked));

            simulation.Environment.SendClick(psn, e);

            if (e.ChangedButton == MouseButton.Left) selector.OnLeftMouseUp(psn.X, psn.Y);
        }

        private void overlayImage_DragOver(object sender, DragEventArgs e)
        {

        }

        public double HeatMin
        {
            get
            {
                return this.currentHeatMapSource.Min;
            }
            set
            {
                if (value > this.currentHeatMapSource.Max) this.currentHeatMapSource.Max = value+1E-6;
                this.currentHeatMapSource.Min = value;
            }
        }
        public double HeatMax
        {
            get
            {
                return this.currentHeatMapSource.Max;
            }
            set
            {
                if (value < this.currentHeatMapSource.Min) this.currentHeatMapSource.Min = value-1E-6;
                this.currentHeatMapSource.Max = value;
            }
        }

        private void MinMaxBox_KeyEnterUpdate(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox tBox = (TextBox)sender;

                DependencyProperty prop = TextBox.TextProperty;

                BindingExpression binding = BindingOperations.GetBindingExpression(tBox, prop);
                if (binding != null) { binding.UpdateSource(); }
            }
        }
    }
}
