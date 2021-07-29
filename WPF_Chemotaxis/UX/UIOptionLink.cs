using System;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Chemotaxis.UX
{
    public class UIOptionLink 
    {

        protected ObservableCollection<object> options = new();
        protected Object selectedItem;

        public virtual object SelectedItem
        {
            get
            {
                return selectedItem;
            }
            set
            {
                selectedItem = value;
            }
        }
  
        public virtual ObservableCollection<object> Options
        {
            get
            {
                return options;
            }
        }
        protected string label;
        protected object target;
        protected FieldInfo field;
        protected Type type;

        protected bool nullable;

        public virtual string Label
        {
            get
            {
                return label;
            }
        }

        public UIOptionLink(string label, object target, FieldInfo field, bool nullable)
        {
            this.label = label;
            this.target = target;
            this.field = field;
            this.nullable = nullable;
            this.type = field.FieldType;
        }
    }
}
