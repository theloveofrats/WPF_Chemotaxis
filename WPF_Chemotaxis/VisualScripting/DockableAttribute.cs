using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Chemotaxis.VisualScripting
{
    //Annotation for creating dockable VS elements. Assumes this is child if child is not found.
    internal class DockableAttribute : Attribute
    {
        public double dockDistance = 25;
        public string parentPropertyName;
        public string childPropertyName = "";
    }
}
