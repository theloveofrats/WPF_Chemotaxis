using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Chemotaxis.UX
{
    public class InstanceChooserAttribute : Attribute
    {
        public string label { get; set; }
    }
}
