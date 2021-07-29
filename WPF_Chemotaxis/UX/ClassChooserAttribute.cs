using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Chemotaxis.UX
{
    class ClassChooserAttribute : Attribute
    {
        public string label { get; set; }
        public Type baseType; 
    }
}
