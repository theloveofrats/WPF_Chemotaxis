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

        /// <summary>
        /// When connecting two elements, the parent is first asked if it has a preferred method of adding an ILinkable of this type.
        /// This is so each type can control it's own method of integrating components. For extensibility, the responsibility can be reversed,
        /// and each ILinkable class can define its own way to integrating itself with a target ILinkable.
        /// 
        /// So now I have this- tryaddto- but actually, each ILinkble should probably register its own adder so the menu automatically builds on extension.
        /// Hmmm....
        /// </summary>
        /// <param name="otherLink">The ILinkable you want to integrate _this with</param>
        /// <returns>true on success, false on failure to integrate. Required so that the UI knows how to respond.</returns>
        public bool TryAddTo(ILinkable otherLink);
    }
}
