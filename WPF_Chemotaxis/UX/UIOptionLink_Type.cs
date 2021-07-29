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
    public class UIOptionLink_Type : UIOptionLink
    {
        private Type selectedType;

        public override object SelectedItem
        {
            get
            {
                return selectedType;
            }
            set
            {
                Type newType = value as Type;
                if (newType == null) return;
                
                ChooseType(newType);
                selectedType = newType;
            }
        }

        public UIOptionLink_Type(string label, object target, FieldInfo field, bool nullable) : base(label, target, field, nullable)
        {
            this.type = field.FieldType;
            Type baseType = this.type;

            if (this.type == typeof(Type) && field.GetCustomAttribute<ClassChooserAttribute>().baseType != null) 
            {
                baseType = field.GetCustomAttribute<ClassChooserAttribute>().baseType;
            }

            FindClasses(baseType);

            var val = field.GetValue(target);
            
            if (val != null)
            {
                if (field.FieldType == typeof(Type))
                {
                    
                    SelectedItem = val;
                }
                else SelectedItem = val.GetType();
            }
            else
            {
                SelectedItem = null;
            }
        }

        private void FindClasses(Type baseType)
        {
            options.Clear();

            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                   .Where(p => baseType.IsAssignableFrom(p) && !p.IsAbstract && !p.IsInterface);

            if (nullable) options.Add(null);

            foreach (Type t in types)
            {
                options.Add(t);
            }
        }

        public void ChooseType(Type type)
        {
            if (this.type == typeof(Type) && field.GetCustomAttribute<ClassChooserAttribute>().baseType != null)
            {
                //Don't need to check, we're not creating instances, just setting a type variable.
                field.SetValue(target, type);
            }
            else
            {

                if (type == null)
                {
                    field.SetValue(target, null);
                    return;
                }

                var currentClass = field.GetValue(target);

                if (currentClass != null)
                {
                    if (currentClass.GetType() == type) return; //Already is the chosen type!

                    // Make sure to remove it from the object model if it's an ILinkable, it's a helper, not a parameter-incluing thing.
                    ILinkable link = (ILinkable)currentClass;

                    if (link != null && Model.Model.MasterElementList.Contains(link))
                    {
                        Model.Model.MasterElementList.Remove(link);
                    }
                }
                field.SetValue(target, Activator.CreateInstance(type));
            }
        }
    }
}
