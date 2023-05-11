using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WPF_Chemotaxis.Model;
using WPF_Chemotaxis.Simulations;
using WPF_Chemotaxis.UX;
using WPF_Chemotaxis.VisualScripting;
using System.Windows.Media.Effects;
using System.Windows.Documents;

namespace WPF_Chemotaxis
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private OpenFileDialog dia = new();
        private SimulationSettings sim;
        private EnvironmentSettings env;

        private static List<Assembly> assemblyList;

        private const string pluginPath = "\\Plugins";
        private const string workingModelFile = "\\SettingsAutosave.json";

        private static readonly object locker = new object();

        private string SaveDirectoryDisplay
        {
            get
            {
                if (sim != null)
                {
                    return sim.SaveDirectory;
                }
                else return "";
            }
            set
            {
                sim.SaveDirectory = value;
                saveDirLabel.Content = string.Format("Save Directory: "+sim.SaveDirectory);
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            LoadPlugins();
            mazeFileThumbnail.MouseLeftButtonUp += (s, e) => SetRegionWithDropper(s, e);
            selectedRegionType.TextChanged += (s, e) =>
            {
                if (RegionType.CurrentlySelected != null)
                {
                    RegionType.CurrentlySelected.Name = selectedRegionType.Text;
                }
            };
            dia.FileOk += (a, b) => this.OnMazeFileChosen(dia.FileName);
            dia.Filter = "PNG image|*.png|All files|*.*";
            dia.RestoreDirectory = true;

            sim = new SimulationSettings();
            env = new EnvironmentSettings();
            LinkSimulation(sim);
            LinkEnvironment(env);
        }


        private void LoadPlugins()
        {
            if (assemblyList == null)
            {
                assemblyList = new List<Assembly>(new Assembly[] { Assembly.GetExecutingAssembly() });
                string basePath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                foreach (string dll in System.IO.Directory.GetFiles(basePath + pluginPath, "*.dll"))
                {
                    System.Diagnostics.Debug.Print(" "); 
                    System.Diagnostics.Debug.Print("PLUGIN LOADED: Assembly named " + dll);
                    System.Diagnostics.Debug.Print(" ");
                    assemblyList.Add(Assembly.LoadFrom(dll));
                }
                //foreach (Assembly dll in assemblyList)
                //{
                //    AppDomain.CurrentDomain.Load(dll.GetName());
                //}
            }
            
        }

        private void SetRegionWithDropper(object source, MouseButtonEventArgs e)
        {
            Color clr;

            Image img = source as Image;

            if (img == null) return;


            if (TryGetColor(img, e.GetPosition(img), out clr))
            {
                RegionType.CurrentlySelected = RegionType.GetRegionType(clr);
                //Then update UI to reflect this change!
                if (RegionType.CurrentlySelected == null)
                {
                    selectedRegionType.Text = "No region selected";
                    regionRules.ItemsSource = null;
                }
                else { 
                    selectedRegionType.Text = RegionType.CurrentlySelected.Name;
                    regionRules.ItemsSource = RegionType.CurrentlySelected.Rules;
                }
            }
        }

        private void SetSaveDirClick(object source, RoutedEventArgs e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            if (dialog.ShowDialog(this).GetValueOrDefault())
            {
                SaveDirectoryDisplay = dialog.SelectedPath;
            }
        }

        private bool TryGetColor(Image s, Point p, out Color c)
        {

            
            if (s.Source == null) return false;

            WriteableBitmap bmp = new WriteableBitmap(s.Source as BitmapSource);

            if (bmp == null) return false;

            double scale = Math.Min(s.ActualWidth / bmp.PixelWidth, s.ActualHeight / bmp.PixelHeight);
            int px = (int)(p.X/scale);
            int py = (int)(p.Y/scale);

            if (px > bmp.PixelWidth) return false;
            if (py > bmp.PixelHeight) return false;

            c = bmp.GetPixel(px, py);
            return true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Model.Model crown = new Model.Model();
            string basePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData) + "\\Dynad Simulations";
                //System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (File.Exists(basePath + workingModelFile))
            {
                //crown.DeserializeModelAtPath(basePath + workingModelFile, true);

                DeserialiseAutosave(basePath + workingModelFile);
                simParameters.Items.Refresh();
            }
            ResetLinks();
            

            RegionType.RehookAllUpdateEvents(regionRules);
            btn_NewSim.IsEnabled = (File.Exists(env.ImagePath));
            SetUpVisualScriptingWindow();

            Model.Model.Current.PropertyChanged += (s, e) => this.DisplayLink();
        }

        private void DeserialiseAutosave(string autosavePath, bool clean=true)
        {
            if (clean)
            {
                Clear_Model(new object(), new RoutedEventArgs());
                Model.Model.FreezeAdditions = false;
            }
            else
            {
                Model.Model.FreezeAdditions = true;
            }

            string dirPath = System.IO.Path.GetDirectoryName(autosavePath);

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            using (Autosave autosave = Autosave.ReadFromFile(autosavePath))
            {
                if (clean)
                {
                    foreach (Color clr in autosave.Regions.Keys)
                    {
                        RegionType.AddRegionType(clr, autosave.Regions[clr]);
                    }
                    this.sim.LoadParameterValues(autosave.miscParams);
                    this.env.DX = autosave.miscParams.dx;
                    if (autosave.saveDirPath != null) this.SaveDirectoryDisplay = autosave.saveDirPath;
                    if (autosave.mazePath != null)
                    {
                        dia.FileName = autosave.mazePath;
                        OnMazeFileChosen(autosave.mazePath);
                    }
                }
                else
                {
                    foreach (ILinkable link in autosave.ModelElements)
                    {
                        //Delete this whole bundle!
                        System.Diagnostics.Debug.Print("Checking added ILinkable named "+link.Name);

                        bool admit = true;
                        foreach(var currentLink in Model.Model.MasterElementList)
                        {
                            if (currentLink.Name != null)
                            {
                                System.Diagnostics.Debug.Print(String.Format("Comparing {0} {1} and {2} {3}::{4}", link.DisplayType, link.Name, currentLink.DisplayType, currentLink.Name, ((currentLink.Name == link.Name) && (currentLink.DisplayType == link.DisplayType)).ToString()));
                            }
                            if ((currentLink.Name == link.Name) && (currentLink.DisplayType == link.DisplayType))
                            {
                                admit = false;
                                break;
                            }
                        }
                        if (admit)
                        {
                            Model.Model.MasterElementList.Add(link);
                            // Do we need a trigger here that connects missing links to names with the correct names in the new setup? #
                            // Perhaps a Re-link function that points all your current referees to a new ILinkable? 
                            foreach (ILinkable connection in link.LinkList)
                            {
                                foreach (var alternativeConnection in Model.Model.MasterElementList)
                                {
                                    if (alternativeConnection.DisplayType == connection.DisplayType && alternativeConnection.Name == connection.Name)
                                    {
                                        link.RemoveElement(connection, alternativeConnection);
                                    }
                                }
                            }
                        }
                    }
                }
                Model.Model.FreezeAdditions = false;
            }
        }

        private void SerialiseAutosave(string autosavePath)
        {
            Autosave newSave = new Autosave();

            string dirPath = System.IO.Path.GetDirectoryName(autosavePath);

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            newSave.ModelElements = Model.Model.MasterElementList.ToList();
            newSave.Regions = new();

            if (mazeFileThumbnail.Source != null && env.ImagePath!=null && env.ImagePath!="") {

                WriteableBitmap bmp = new WriteableBitmap(mazeFileThumbnail.Source as BitmapSource);
                if (bmp != null)
                { 
                    foreach (var entry in RegionType.GetRegionTypes)
                    {
                        if (entry.Value.Rules.Count() > 0)
                        {
                            if (ContainsColor(bmp, entry.Key))
                            {
                                System.Diagnostics.Debug.Print(string.Format("Found color {0} for regiontype {1}.", entry.Key, entry.Value.Name));
                                newSave.Regions.Add(entry.Key, entry.Value);
                            }
                        }
                    }

                }
            }




            newSave.miscParams = new MiscParamTable(env.DX, sim.duration, sim.dt, sim.out_freq);

            if (Directory.Exists(sim.SaveDirectory)) newSave.saveDirPath = sim.SaveDirectory;
            if (File.Exists(dia.FileName)) newSave.mazePath = dia.FileName;

            string saveString = JsonConvert.SerializeObject(newSave, Formatting.Indented,
                 new JsonSerializerSettings
                 {
                     TypeNameHandling = TypeNameHandling.Auto,
                     ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                     PreserveReferencesHandling = PreserveReferencesHandling.All
                 });
            File.WriteAllText(autosavePath, saveString);
        }

        private bool ContainsColor(WriteableBitmap checkbmp, Color clr)
        {
            for (int i = 0; i < checkbmp.PixelWidth; i++)
            {
                for (int j = 0; j < checkbmp.PixelHeight; j++)
                {
                    if (checkbmp.GetPixel(i, j).Equals(clr)) return true;
                }
            }
            return false;
        }

        private void Window_Closed(object sender, CancelEventArgs e)
        {
            if (Simulation.Current != null)
            {
                Simulation.Current.Cancel();
            }
            string basePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData) + "\\Dynad Simulations";
            //string basePath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            SerialiseAutosave(basePath + workingModelFile);

            //Model.Model.Current.SerializeModelToPath(basePath + workingModelFile);
        }

        private void DisplayLink()
        {
            ILinkable link = Model.Model.CurrentFocus;
            selectedElementLabel.Content = link.Name;
            SetCurrentElements(link.LinkList);
            SetParams(link);
            SetOpts(link);
            MainTabControl.SelectedIndex = 0;
        }

        private void ResetLinks()
        {
            Model.Model.Reset();
            DisplayLink();
        }

        private void ChooseLink(ILinkable link)
        {
            Model.Model.SetNextFocus(link);
            DisplayLink();
        }

        private void LinkSimulation(SimulationSettings sim)
        {
            simParameters.ItemsSource = sim.ParamList;
        }
        private void LinkEnvironment(EnvironmentSettings env)
        {
            envParameters.ItemsSource = env.ParamList;
        }
        private void SetCurrentElements(ObservableCollection<ILinkable> elementList)
        {
            currentElements.ItemsSource = elementList;
        }
        private void SetParams(ILinkable link)
        {
            currentParams.ItemsSource = link.ParamList;
        }
        private void SetOpts(ILinkable link)
        {
            ObservableCollection<UIOptionLink> opts = link.OptsList;
            currentChoices.ItemsSource = opts;
        }


        // TURNING THIS INTO AN ADD ELEMENT BUTTON- REQUIRES A KNOWLDGE OF CURRENT FOCUSSED ELEMENT AND MAYBE ELEMENT HISTORY FOR FORWARD AND BACK BUTTONS.
        private void Add_Cell_Click(object sender, RoutedEventArgs e)
        {
            //CellType.cellTypes.Add(new CellType());
            //currentElements.Items.Refresh();

            popAddElement.IsOpen = true;

            popUpAddElementItemsList.Items.Clear();
            List<Button> addButtons = GetCurrentButtons();
            foreach (Button btn in addButtons) {
                popUpAddElementItemsList.Items.Add(btn);
            }
            //Add controls to the popup based on current link focus:

        }

        private void Add_Rule_Click(object sender, RoutedEventArgs e)
        {
            if (RegionType.CurrentlySelected == null) return;
            popAddRule.IsOpen = true;

            popUpRuleList.Items.Clear();
            List<Button> addButtons = GetRuleButtons();
            foreach (Button btn in addButtons)
            {
                popUpRuleList.Items.Add(btn);
            }
        }

        private void Remove_Rule_Click(object sender, RoutedEventArgs e)
        {
            if (RegionType.CurrentlySelected == null) return;
           
            RegionType.CurrentlySelected.DeleteRuleAtIndex(regionRules.SelectedIndex);
        }

        private void Clear_Model(object sender, RoutedEventArgs e)
        {
            foreach (ILinkable obj in Model.Model.MasterElementList)
            {
                
                // Could do with an obj.Clean() function to mop up in case of mutual references causing memory leak.
            }
            Model.Model.MasterElementList.Clear();

            RegionType.ClearRegions();
            OnMazeFileChosen("");
        }

        // Generates buttons for all current elements of the type elementListFilterType, and links them to the MethodInfo method. These methods MUST take a single argument of the correct type! 
        private void ElementAdderButtonClick(object button, object originator, MethodInfo method, Type elementListFilterType)
        {
           
            // We are currently clearing the popup and repopulating it- this might change, in which case drop this line
            popUpAddElementItemsList.Items.Clear();

            if (elementListFilterType.IsAssignableTo(typeof(ILinkable)))
            {
                IEnumerable<ILinkable> opts = Model.Model.MasterElementList.Where(opt => opt.GetType() == elementListFilterType);

                foreach (ILinkable opt in opts)
                {
                    Button btn = new();
                    btn.Content = opt.Name;
                    btn.Click += (sender, e) =>
                    {
                        method.Invoke(originator, new object[] { opt });
                        popAddElement.IsOpen = false;
                        DisplayLink();
                    };
                    popUpAddElementItemsList.Items.Add(btn);
                }
                Button newButton = new();
                newButton.Content = "New ...";
                newButton.Click += (sender, e) =>
                {
                    ILinkable newObj = Activator.CreateInstance(elementListFilterType) as ILinkable;
                    newObj.Name = "New " + newObj.DisplayType;
                    method.Invoke(originator, new object[] { newObj });
                    popAddElement.IsOpen = false;
                    DisplayLink();
                };
                popUpAddElementItemsList.Items.Add(newButton);
            }
            else
            {
                var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                  .Where(p => elementListFilterType.IsAssignableFrom(p) && !p.IsAbstract && !p.IsInterface);

                foreach(Type t in types)
                {
                    Button btn = new();
                    btn.Content = t.Name;
                    btn.Click += (sender, e) =>
                    {
                        var obj = Activator.CreateInstance(t);
                        method.Invoke(originator, new object[] { obj });
                        popAddElement.IsOpen = false;
                        DisplayLink();
                    };
                    popUpAddElementItemsList.Items.Add(btn);
                }
            }
        }


        private List<Type> regionRuleTypes;

        private List<Button> GetRuleButtons()
        {
            List<Button> buttons = new();

            if (regionRuleTypes == null)
            {
                regionRuleTypes = new();
                regionRuleTypes.AddRange(AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).Where((type) => type.IsSubclassOf(typeof(RegionRule)) && !type.IsAbstract));

            }

            //working example that imports from other assemblies?
            //
            //var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
            //      .Where(p => elementListFilterType.IsAssignableFrom(p) && !p.IsAbstract && !p.IsInterface);


            foreach (Type rule in regionRuleTypes)
            {
                Button button = new Button();
                button.Content = (Activator.CreateInstance(rule) as RegionRule).DisplayName;
                buttons.Add(button);
                button.Click += (sender, e) => RuleAdded(Activator.CreateInstance(rule) as RegionRule);
            }
            return buttons;
        }

        private void RuleAdded(RegionRule newRule)
        {
            if (RegionType.CurrentlySelected == null) return;
            popAddRule.IsOpen = false;
            RegionType.CurrentlySelected.AddRule(newRule);
            newRule.OptionsUpdated += (s, e) => CollectionViewSource.GetDefaultView(regionRules.ItemsSource).Refresh();
           
        }


        private List<Button> GetCurrentButtons()
        {
            List<Button> buttons = new();

            // Here we list all the methods that explicitly allow you to add elements!
            IEnumerable<MethodInfo> methods = Model.Model.CurrentFocus.GetType().GetMethods().Where(method => method.GetCustomAttributes<ElementAdder>().Any());
            System.Diagnostics.Debug.WriteLine(string.Format("in type {0}. ElementAdder fields.Count = {1}", Model.Model.CurrentFocus.GetType(), methods.Count()));
            foreach (MethodInfo method in methods)
            {
                ElementAdder adder = (method.GetCustomAttribute<ElementAdder>() as ElementAdder);
                Button button = new Button();
                button.Content = adder.label;
                buttons.Add(button);
                button.Click += (sender, e) => ElementAdderButtonClick(sender, Model.Model.CurrentFocus, method, adder.type);
            }

            //Here we add buttons for creating basic versions of other ILinkables from plugins that only make sense in the bottom of the heirarchy. This only applies to the master
            //element list, not other model levels!

            if (Model.Model.CurrentFocus == Model.Model.Current)
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {

                    System.Diagnostics.Debug.WriteLine(string.Format("Scanning assembly {0}", assembly));


                    foreach (Type type in assembly.GetTypes().Where(type => type.GetCustomAttributes<CustomBaseElementAttribute>().Any()))
                    {
                        System.Diagnostics.Debug.WriteLine(" ");
                        System.Diagnostics.Debug.WriteLine(string.Format("ASSEMBLY {0} CONTAINS TYPE {1}", assembly, type));
                        System.Diagnostics.Debug.WriteLine(" ");


                        CustomBaseElementAttribute atr = type.GetCustomAttribute<CustomBaseElementAttribute>();
                        Button button = new Button();
                        button.Content = atr.newElementButtonLabel;
                        buttons.Add(button);
                        button.Click += (sender, e) =>
                        {
                            ILinkable newObj = Activator.CreateInstance(type) as ILinkable;
                            newObj.Name = "New " + newObj.DisplayType;
                            //method.Invoke(originator, new object[] { newObj });
                            popAddElement.IsOpen = false;
                            DisplayLink();
                        };
                    }
                }
            }


            return buttons;
        }

        private void OnMouseLeavePopAddElement(object sender, RoutedEventArgs e)
        {
            popAddElement.IsOpen = false;
        }

        private void OnMouseLeavePopAddRule(object sender, RoutedEventArgs e)
        {
            popAddRule.IsOpen = false;
        }

        private void On_Click_Link(object sender, RoutedEventArgs e)
        {
            ILinkable selectedLink = (ILinkable)currentElements.SelectedItem;
            ChooseLink(selectedLink);
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            ResetLinks();
        }
        private void ForeButton_Click(object sender, RoutedEventArgs e)
        {
            ILinkable newFocus = Model.Model.FocusForward;
            DisplayLink();

        }
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ILinkable newFocus = Model.Model.FocusBack;
            DisplayLink();
        }

        private void TabControl_SelectionChanged(object sender, RoutedEventArgs e)
        {

        }

        private void Browse_Sim_File_Button_Click(object sender, RoutedEventArgs e)
        {
            dia.ShowDialog();
        }
        private void OnMazeFileChosen(string path)
        {
            BitmapImage thumb = new BitmapImage();
            if (File.Exists(path)) { 
                thumb.BeginInit();
                thumb.UriSource = new Uri(path);
                thumb.EndInit();
            }
            env.ImagePath = path;
            mazeFileThumbnail.Source = thumb;

            mazeFileNameLabel.Content = System.IO.Path.GetFileName(path);

            btn_NewSim.IsEnabled = (File.Exists(path));
        }

        private void deleteItem_Click(object sender, RoutedEventArgs e)
        {
            ILinkable selected = currentElements.SelectedItem as ILinkable;

            Model.Model.CurrentFocus.RemoveElement(selected);

            if (!Model.Model.MasterElementList.Contains(Model.Model.CurrentFocus)) ChooseLink(Model.Model.Current);
            DisplayLink();
        }

        private void SerializeModel_Click(object sender, RoutedEventArgs e)
        {
            //Not working- this was used to separately save the model but this is no longer right, as in general we want to save or load the experiment, not just the model.
            //Model.Model.Current.SerializeModel();

            //Make a save dialog
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.DefaultExt = "json";
            saveFileDialog.AddExtension = true;
            saveFileDialog.Filter = "JSON | *.json";
            if (saveFileDialog.ShowDialog() == true)
            {
                SerialiseAutosave(saveFileDialog.FileName);
            }
        }
        private void DeserializeModel_Click(object sender, RoutedEventArgs e)
        {

            //Model.Model.Current.DeserializeModel(true);
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.DefaultExt = "json";
            dialog.AddExtension = true;
            dialog.Filter = "JSON | *.json";
            if (dialog.ShowDialog() == true)
            {
                if (!File.Exists(dialog.FileName)) return;
                DeserialiseAutosave(dialog.FileName);
            }
        }



        private void DeserializePlusModel_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.DefaultExt = "json";
            dialog.AddExtension = true;
            dialog.Filter = "JSON | *.json";
            if (dialog.ShowDialog() == true)
            {
                if (!File.Exists(dialog.FileName)) return;
                DeserialiseAutosave(dialog.FileName, false);
            }
        }

        private void Run_Sim_Button_Click(object sender, RoutedEventArgs e)
        {
            //Needs to be changed to link simulation, and unlink when sim finishes, not create it. One at a time is fine! We can put repeats in a UI.
            string newTargetDirectory;
            sim.MakeNewSimDirectory(out newTargetDirectory);
            if (newTargetDirectory != null)
            {
                string modelPath = newTargetDirectory + "\\StartingSetup.json";
                SerialiseAutosave(modelPath);
            }

            if (Simulation.Current == null)
            {
                Simulation newSim = Simulation.StartSimulation(sim, env, newTargetDirectory);
                newSim.Redraw += (s, e, m) => UpdateTime(newSim.Time);
                newSim.Start();
            }
            AddDisplayView(Simulation.Current);
        }

        private void UpdateTime(double time)
        {
            string newTime = TimeSpan.FromMinutes(time).ToString(@"hh\:mm\:ss");
            Dispatcher.Invoke(() => timeLabel.Content = newTime);
        }

        private void PlayPause_Button_Click(object sender, RoutedEventArgs e)
        {
            if (btn_PlayPause.Content == FindResource("Play"))
            {
                btn_PlayPause.Content = FindResource("Pause");
                sim.OnResume(sender, e);
            }
            else
            {
                btn_PlayPause.Content = FindResource("Play");
                sim.OnPause(sender, e);
            } 
        }

        ///<summary>
        ///Opens a new display window, running on a new thread- this is to avoid the windows blocking one another.
        ///</summary>
        private void AddDisplayView(Simulation newSim)
        {
            Task.Factory.StartNew(new Action(() =>
            {
                    DisplayWindow displayWindow = new DisplayWindow();
                    displayWindow.LinkSimulation(newSim, chartStack);
                    displayWindow.Show();
                    displayWindow.Start();
            }), 
            CancellationToken.None, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void ColorDropperButton_Click(object sender, RoutedEventArgs e)
        {
            
        }


        /* Visual Scripting View Model Section
         * 
         * 
         * 
         * 
         * 
         * 
         *
         * 
         * 
         */


        /*
         *   INITIALISE VISUAL SCRIPTING AND CONNECT LISTS TO VISUALS IN THAT TAB.
         */
        private VSModelManager modelManager;

        private void SetUpVisualScriptingWindow()
        {
            VSCanvas.KeyDown += KeyDownHandler;
            SetVSElementsDisplaySource();

            VisualScriptingSelectionManager.InitialiseVisualScriptingSelectionManager(VSCanvas);
            modelManager = VSModelManager.Current;
            modelManager.Init(VSCanvas);
        }

        private void SetVSElementsDisplaySource()
        {
            visualElementList.ItemsSource = VSElementList;
        }

        //Handle changes to selection. selection should not be changed elsewhere
        private void visualElementList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VisualScriptingSelectionManager.Current.SelectMenuItem(visualElementList.SelectedItem as VSListMenuElement);
        }

        private ObservableCollection<VSListMenuElement> vsElementsList;
        public ObservableCollection<VSListMenuElement> VSElementList 
        {
            get
            {
                if (vsElementsList == null) vsElementsList = FindAllVSElements();
                return vsElementsList;
            }
            private set
            {

            }
        }
        
        private void KeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete) { 
                if (VisualScriptingSelectionManager.Current.HasSelection)
                {
                    VSModelManager.Current.TryDeleteModelElement(VisualScriptingSelectionManager.Current.SelectedElement);
                }
            }
        }

        private ObservableCollection<VSListMenuElement> FindAllVSElements()
        {
            ObservableCollection<VSListMenuElement> viewList = new();

            var typeList = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where(y => y.GetCustomAttribute<VSElementAttribute>()!=null);

            foreach (var iterType in typeList)
            {
                var vsInfo = iterType.GetCustomAttribute<VSElementAttribute>();

                viewList.Add(new VSListMenuElement(vsInfo.ui_TypeLabel, vsInfo.symbolResourcePath, vsInfo.symbolSize, iterType, new Point(vsInfo.tagX, vsInfo.tagY), vsInfo.tagCentre));
            }
            return viewList;
        }



        private static T InvokeMethod<T>(Type type, string methodName, object obj = null, params object[] parameters) => (T)type.GetMethod(methodName)?.Invoke(obj, parameters);

        //internal VisualScriptingSelectionManager VSViewManager { get; private set;}

        private void VSCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double angle = e.Delta/8d;
            if (VisualScriptingSelectionManager.Current.HasSelection)
            {
                VisualScriptingSelectionManager.Current.RotateSelected(angle);
            }
        }
        /*
         * MOUSE DOWN HANDLERS FOR HARD_CODED PARTS OF THE VIEW. CAVNAS CAPURES CLICKS TO PROCESS MOUSEUP WITHOUT ERRORS.
         * 
         */


        // This handles clicking on the MENU. Cancels VSElement selection.
        private void VSMenuItem_LeftMouseDown(object sender, MouseButtonEventArgs e)
        {
            VisualScriptingSelectionManager.Current.ClearSelection();
            //Mouse.Capture(VSCanvas, CaptureMode.SubTree);
            //System.Diagnostics.Debug.Print("CANVAS CAPTURE");
        }


        // Click on canvas. Cancels menu and VS element selection.
        private void VSCanvas_LeftMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled) return;
            VisualScriptingSelectionManager.Current.ClearSelection();
            visualElementList.UnselectAll();
            VisualScriptingSelectionManager.Current.StartDrag(e.GetPosition(VSCanvas));
        }

      

        // Drag on canvas
        private void VSCanvas_LeftMouseDrag(object sender, MouseEventArgs e)
        {
            Point clickPsn = e.GetPosition(VSCanvas);
            if (VisualScriptingSelectionManager.Current.InBounds(clickPsn))
            {
                VisualScriptingSelectionManager.Current.UpdateDrag(clickPsn);
                if (VisualScriptingSelectionManager.Current.IsDragging) 
                {
                    Mouse.Capture(VSCanvas, CaptureMode.SubTree);
                }
            }
        }

        /*
         * MOUSE UP HANDLER ON CANVAS
         * 
         */
        private void VSCanvas_LeftMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled) return;
            VSListMenuElement selectedSidebarItem = (visualElementList.SelectedItem as VSListMenuElement);
            Point clickPsn = e.GetPosition(VSCanvas);

            if (VisualScriptingSelectionManager.Current.InBounds(clickPsn))
            {
                // If we clicked in bounds and there is a selected menu item, create a new object on the canvas
                if (selectedSidebarItem != null)
                {
                    modelManager.CreateNewModelElementFromMenu(selectedSidebarItem, clickPsn);
                    visualElementList.UnselectAll();
                }
                // Otherwise, if we are in bounds and we were dragging a selected element
                else if (VisualScriptingSelectionManager.Current.HasSelection && VisualScriptingSelectionManager.Current.IsDragging)
                {
                    VisualScriptingSelectionManager.Current.MoveSelected(clickPsn);
                }

            }
            ResetMouseState();
            e.Handled = true;
        }

        /*
        private ICollection<FrameworkElement> GetChildHits(FrameworkElement parent, Point point)
        {
            List<FrameworkElement> hits = new();
            for(int n=0; n<VisualTreeHelper.GetChildrenCount(parent); n++)
            {
                var child = VisualTreeHelper.GetChild(parent, n);
                if(child.GetType().IsAssignableTo(typeof(FrameworkElement)))  
                {
                    //Add all subhits of a child, even if it manages to avoid inclusion itself- a receptor might stick out of a cell, for example!
                    FrameworkElement feChild = (FrameworkElement)child;
                    var subhits = GetChildHits(feChild, point);
                    hits.AddRange(subhits); 
                    if (VSViewManager.InBounds(point, feChild))
                    {
                        if (hits == null) hits = new();
                        hits.Add(feChild);
                    }
                }
            }
            return hits;
        }
        */

        private void ResetMouseState()
        {
            VisualScriptingSelectionManager.Current.EndDrag();
        }


    }
}
