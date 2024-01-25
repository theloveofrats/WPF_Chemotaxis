using System;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Chemotaxis.UX;
using Newtonsoft.Json;
using System.ComponentModel;

namespace WPF_Chemotaxis.Simulations
{
    /// <summary>
    /// Table of general values shared across instances of the Environment class, linking it to the UI.
    /// </summary>
    public class EnvironmentSettings : INotifyPropertyChanged
    {
        [ClassChooser(label = "Fluid model")]
        public IFluidModel fluidModel;

        [Param(Name = "dx")]
        public double DX { get; set; } = 4;

        private string imagePath;

        public event PropertyChangedEventHandler PropertyChanged;

        public string ImagePath
        {
            get
            {
                return imagePath;
            }
            set
            {
                imagePath = value;  
            }
        }

        [JsonIgnore]
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
