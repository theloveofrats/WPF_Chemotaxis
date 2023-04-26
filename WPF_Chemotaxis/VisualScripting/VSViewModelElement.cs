using System;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WPF_Chemotaxis.VisualScripting
{
    public class VSViewModelElement
    {
        public string IconResourcePath { get; private set; }
        public string UIDisplayLabel { get; private set; }
        public Type   TargetType     { get; private set; }

        public VSViewModelElement(string UIDisplayLabel, string IconResourcePath, Type TargetType)
        {
            this.UIDisplayLabel = UIDisplayLabel;
            this.IconResourcePath = IconResourcePath;
            this.TargetType = TargetType;
            String FilePath = @"C:\Users\UserName\Documents\Map.txt";
        }
    }
}
