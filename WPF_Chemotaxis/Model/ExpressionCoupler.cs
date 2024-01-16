using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using WPF_Chemotaxis.UX;
using WPF_Chemotaxis.VisualScripting;

namespace WPF_Chemotaxis.Model
{
    [Dockable(childPropertyName = "ChildComponent", parentPropertyName = "Cell")]
    public class ExpressionCoupler : LabelledLinkable
    {
        [JsonProperty]
        [Link]
        public ILinkable ChildComponent { get; private set; }
        [JsonProperty]
        [Link] 
        public CellType Cell { get; private set; }

        [Param(Name = "Basal weight", Min = 0)]
        public CenteredDoubleRange BasalWeight { get; set; } = new CenteredDoubleRange(1, 0);
        public ExpressionCoupler() : base() 
        {
            Init();
        }
        public ExpressionCoupler(ILinkable Component, CellType Cell)
        {
            this.Cell = Cell;
            this.ChildComponent = Component;
            Init();
        }

        public override void RemoveElement(ILinkable element, ILinkable replacement = null)
        {
           if (element is CellType)
           {
                CellType ct = (CellType)element;
                if (this.Cell == ct)
                {
                    this.Cell = (CellType)replacement;
                    if (this.Cell == null) Model.Current.RemoveElement(this);
                }
           }
           if (element == this.ChildComponent)
           {
                this.ChildComponent = null;
                // Only allow like for like replacement!
                if(replacement!=null && element.GetType() == replacement.GetType())
                {
                    this.ChildComponent = replacement;    
                }
                if(this.ChildComponent==null) Model.Current.RemoveElement(this);
            }
        }


        public override string Name { 
            get {if (Cell == null || ChildComponent == null)
            {
                return "Broken expression coupler";
            }
            else return string.Format("{0} expression of {1}", Cell.Name, ChildComponent.Name); } 
            set { return; } 
        }
    }
}
