using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Chemotaxis.UX;

namespace WPF_Chemotaxis.VisualScripting
{
    public interface VSFriendlyLinkable : ILinkable
    {
        public static string symbolResource { get; }
        public static double symbolSize { get;  }
    }
}
