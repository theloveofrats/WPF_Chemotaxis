using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Chemotaxis.VisualScripting
{
    public class LineConnectorAttribute : Attribute
    {
        public string parentPropertyName;
    }
}
