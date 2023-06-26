using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using WPF_Chemotaxis.UX;

namespace WPF_Chemotaxis.VisualScripting
{
    public class VisualLineAttribute : Attribute
    {
        public LineAnchorType parentAnchor  = LineAnchorType.ANCHOR_CENTRE;
        public LineAnchorType childAnchor   = LineAnchorType.ANCHOR_CENTRE;
        public LineHeadType parentArrowHead = LineHeadType.NONE;
        public LineHeadType childArrowHead  = LineHeadType.NONE;
        public string colorFunc = "";

        public double parentAnchorDistance = 0;
        public double childAnchorDistance  = 0;
    }
}
