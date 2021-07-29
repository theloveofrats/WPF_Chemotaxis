using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Chemotaxis.UX
{
    public class Param : Attribute
    {
        public string Name { get; set; } = "Unnamed Parameter";
        public double Min { get; set; } = Double.NaN;
        public double Max { get; set; } = Double.NaN;
    }
}
