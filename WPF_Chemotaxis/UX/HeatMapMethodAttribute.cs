using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Chemotaxis.UX
{
    public class HeatMapMethodAttribute : Attribute
    {
        public string name { get; set; }
        public double min { get; set; }
        public double max { get; set; }
    }
}
