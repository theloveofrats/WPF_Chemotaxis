using System;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Chemotaxis.UX;
using System.ComponentModel;
using System.IO;

namespace WPF_Chemotaxis.Simulations
{
    /// <summary>
    /// Parameter values for simulations that can be shared across multiple runs, for example timestep and duration.
    /// Used to expose alterable parameters in the UI prior to instantiating a simulation (which will need these values
    /// from the get-go).
    /// </summary>
    public class SimulationSettings : INotifyPropertyChanged
    {
        [Param(Name = "Δt (min)")]
        public double dt { get; set; } = 0.0075;

        [Param(Name = "Duration (min)")]
        public double duration { get; set; } = 120.0;

        [Param(Name = "output period (mins)")]
        public double out_freq { get; set; } = 0;

        public string SaveDirectory { get; set; } = "";

        public SimulationSettings()
        {

        }

        public delegate void UIEventHandler(object sender, EventArgs e);
        public event UIEventHandler Pause;
        public event UIEventHandler Resume;
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPause(object sender, EventArgs e)
        {
            if (Pause != null)
            {
                Pause(sender, e);
            }
        }
        public void OnResume(object sender, EventArgs e)
        {
            if (Resume != null)
            {
                Resume(sender, e);
            }
        }

        public void LoadParameterValues(MiscParamTable parms)
        {
            this.duration = parms.duration;
            this.dt = parms.dt;
            this.out_freq = parms.saveInterval;
        }

        public bool MakeNewSimDirectory(out string targetDirectory)
        {
            targetDirectory = null;
            if (out_freq > 0)
            {
                if (Directory.Exists(this.SaveDirectory))
                {
                    targetDirectory = string.Format("{0}\\Sim_{1}\\", this.SaveDirectory, DateTime.Now.ToString("yyMMdd-HH-mm-ss"));
                    Directory.CreateDirectory(targetDirectory);
                   
                }
            }
            return targetDirectory != null && Directory.Exists(targetDirectory); 
        }

        public virtual ObservableCollection<UIParameterLink> ParamList
        {
            get
            {
                ObservableCollection<UIParameterLink> exposedParams = new();
                var paramProps = this.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(pi => pi.IsDefined(typeof(Param), false) && pi.CanWrite);

                foreach (PropertyInfo pi in paramProps)
                {
                    UIParameterLink link = new UIParameterLink(pi, this);
                    exposedParams.Add(link);
                }

                return exposedParams;
            }
        }
    }
}