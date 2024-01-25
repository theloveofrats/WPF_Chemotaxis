using System;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Chemotaxis.Model;

namespace WPF_Chemotaxis.UX
{
    // This is a bit sloppy. The objects should minimum be ILinkables, but I don't explicitly enforce it. Put in generic definition?
    public class UIOptionLink_Instance : UIOptionLink
    {
        private ILinkable selectedInstance;
        public override object SelectedItem
        {
            get
            {
                return selectedInstance;
            }
            set
            {
                ILinkable newInstance = value as ILinkable;
                if (newInstance == null)
                {
                    //System.Diagnostics.Debug.Print("Selected null instance");
                }
                else
                {
                    //System.Diagnostics.Debug.Print("Selected a instance :: " + newInstance.Name);
                }
                ChooseInstance(newInstance);
                selectedInstance = newInstance;
            }
        }

        public UIOptionLink_Instance(string label, object target, PropertyInfo prop, bool nullable) : base(label, target, prop, nullable)
        {

            this.type = prop.PropertyType;
            FindOptions();

            var val = prop.GetValue(target);

            

            if (val != null)
            {
                SelectedItem = val;
            }
            else
            {
                SelectedItem = null;
            }
        }

        private void FindOptions()
        {
            options.Clear();

            if (nullable)
            {
                //System.Diagnostics.Debug.Print("Adding null option.");
                options.Add(null);
            }
            foreach(ILinkable opt in Model.Model.MasterElementList)
            {
                if (this.type.IsAssignableFrom(opt.GetType()))
                {
                    options.Add(opt);
                }
            } 
        }

        public void ChooseInstance(ILinkable choice)
        {
            if (choice == null)
            {
                //System.Diagnostics.Debug.Print("Setting the field to null");
                prop.SetValue(target, null);
            }
            var currentChoice = prop.GetValue(target);

            if (choice==currentChoice) return; //Already chosen!

            else prop.SetValue(target, choice);
        }
    }
}
