using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using WPF_Chemotaxis.UX;

namespace WPF_Chemotaxis.VisualScripting
{
    public class VSRelationAttribute : Attribute
    {
        public ForcedPositionType forcedPositionType; 
        public double forcePositionDistance = 0;
        public string parentPropertyName;
        public string childPropertyName;
        public string workerClassName = "";
    }

    public enum ForcedPositionType { NONE, RADIUS, LIST, WORKERCLASS};
}
