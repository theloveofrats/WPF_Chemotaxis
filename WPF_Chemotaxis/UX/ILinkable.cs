using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WPF_Chemotaxis.UX
{
    public interface ILinkable
    {
        public string Name { get; set; }

        public string DisplayType { get; }

        [JsonIgnore]
        public ObservableCollection<ILinkable> LinkList {get;}

        [JsonIgnore]
        public ObservableCollection<UIParameterLink> ParamList { get; }

        [JsonIgnore]
        public ObservableCollection<UIOptionLink> OptsList { get; }

        public void RemoveElement(ILinkable element, ILinkable replacement = null);
    }
}
