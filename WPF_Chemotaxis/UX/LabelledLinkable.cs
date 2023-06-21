using System;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using WPF_Chemotaxis.Model;
using System.IO;
using System.ComponentModel;
using Newtonsoft.Json.Serialization;

namespace WPF_Chemotaxis.UX
{
    public class LabelledLinkable : ILinkable, INotifyPropertyChanged
    {
        //private string label;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(sender, e);
        }

        public virtual string Name { get; set; }

        //So that any derivated class auomatically adds itself to the masterelementlist.
        public LabelledLinkable()
        {
            
        }

        protected void Init()
        {
            if (!Model.Model.FreezeAdditions)
            {
                Model.Model.MasterElementList.Add(this);
            }
        }

        public LabelledLinkable(string name) : this()
        {
            this.Name = name;
        }
        

        public virtual string DisplayType
        {
            get
            {
                return this.GetType().Name;
            }
        }

        public virtual void RemoveElement(ILinkable element, ILinkable replacement=null)
        {

        }

        public virtual ObservableCollection<ILinkable> LinkList
        {
            get
            {
                ObservableCollection<ILinkable> links = new ObservableCollection<ILinkable>(); 
                var linkFields = this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(fi =>fi.IsDefined(typeof(LinkAttribute), false));
                
                
                
                foreach (FieldInfo fi in linkFields)
                {
                    if (typeof(ILinkable).IsAssignableFrom(fi.FieldType))
                    {
                        var val = fi.GetValue(this);
                        if (val!=null)
                        {
                            links.Add((ILinkable)val);
                        }
                    }
                    else if(fi.GetValue(this)!=null && fi.GetValue(this).GetType().IsAssignableTo(typeof(ILinkable)))
                    {
                        var val = fi.GetValue(this);
                        links.Add((ILinkable)val);
                    }
                    
                    if (fi.FieldType.IsGenericType)
                    {
                        // Check if this is enumerable generic collection of ILinkable
                        if (typeof(IList).IsAssignableFrom(fi.FieldType)) {
                            IList collection = (IList )fi.GetValue(this);

                            collection.GetEnumerator().Reset();

                            foreach (var item in collection)
                            {
                                if (item.GetType().IsAssignableTo(typeof(ILinkable))) { 
                                    ILinkable link = (ILinkable)item;
                                    if (link != null) links.Add(link);
                                }
                            }
                            
                            /*while (collection.GetEnumerator().MoveNext())
                            {
                                ILinkable link = (ILinkable) (collection.GetEnumerator().Current);
                                if(link!=null) links.Add(link);
                            }*/
                        }


                        // Check if this is a dictionary with ILinkable keys
                        else if (typeof(IDictionary).IsAssignableFrom(fi.FieldType))
                        {
                            IDictionary dict = (IDictionary) fi.GetValue(this);
                            foreach (var item in dict.Keys)
                            {
                                ILinkable link = (ILinkable)item;
                                if (link != null) links.Add(link);
                            }

                        }
                    }
                }

                return links;
            }
        }
        public virtual ObservableCollection<UIParameterLink> ParamList
        {
            get
            {
                ObservableCollection<UIParameterLink> exposedParams = new();

                /*var paramFields = this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(fi => fi.IsDefined(typeof(Param), false));

                foreach (FieldInfo fi in paramFields)
                {
                    UIParameterLink link = new UIParameterLink(fi, this);
                    exposedParams.Add(link);
                }
                return exposedParams;*/

                var paramProps = this.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(pi => pi.IsDefined(typeof(Param), false) && pi.CanWrite);

                foreach (PropertyInfo pi in paramProps)
                {
                    UIParameterLink link = new UIParameterLink(pi, this);
                    exposedParams.Add(link);
                }
                return exposedParams;
            }
        }

        //Could not find a part of the path 'H:\Repos\WPF_Chemotaxis\WPF_Chemotaxis\bin\Debug\net5.0-windows\Plugins'.'

        public virtual ObservableCollection<UIOptionLink> OptsList
        {
            get
            {
                ObservableCollection<UIOptionLink> allOpts = new();
                var optFields = this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(fi => fi.IsDefined(typeof(ClassChooserAttribute), false));
                var insFields = this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(fi => fi.FieldType.IsAssignableTo(typeof(ILinkable)) && fi.IsDefined(typeof(InstanceChooserAttribute), false));

               

                foreach (FieldInfo fi in optFields)
                {
                    UIOptionLink link = new UIOptionLink_Type(fi.GetCustomAttribute<ClassChooserAttribute>().label,this, fi, false);
                    allOpts.Add(link);
                }
                foreach (FieldInfo fi in insFields)
                {
                    UIOptionLink link = new UIOptionLink_Instance(fi.GetCustomAttribute<InstanceChooserAttribute>().label, this, fi, true);
                    allOpts.Add(link);
                }
                return allOpts;
            }
        }

        public virtual bool TryAddTo(ILinkable link)
        {
            return false;
        }
    }
}
