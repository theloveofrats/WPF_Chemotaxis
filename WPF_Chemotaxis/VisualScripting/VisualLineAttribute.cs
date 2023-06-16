using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using WPF_Chemotaxis.UX;

namespace WPF_Chemotaxis.VisualScripting
{
    public class VisualLineAttribute : Attribute
    {
        public LineType lineType; 
        public double lineStartDistance = 0;
    }

    public enum LineType {LINE, ARROW_TO, ARROW_FROM};
}
