using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Chemotaxis.UX;

namespace WPF_Chemotaxis.VisualScripting
{
    public class VSElementAttribute : Attribute
    {
        //Might want to move this one into main Iinkable!
        public string ui_TypeLabel;
        public string symbolResourcePath;
        public double symbolSize;

    }
}
