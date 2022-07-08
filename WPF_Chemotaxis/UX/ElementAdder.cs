using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Chemotaxis.UX
{
    // This attribute is used to tag any methods in an ILinkable that are used to add new relationships between model elements.
    // For example, it could be used to tag a method used to add a new receptor relation to a cell.
    // The type specifies the type of new object created by the user- some boilerplate types will be assumed, like CellReceptorRelation.
    // The label specifies the text to display on the button added to the UI, e.g. "Add receptor".

    [AttributeUsage(AttributeTargets.Method)]
    public class ElementAdder : Attribute
    {
        public string label { get; set; }
        public Type type { get; set; }
    }
}
