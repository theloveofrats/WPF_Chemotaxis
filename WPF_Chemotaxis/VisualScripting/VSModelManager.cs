using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WPF_Chemotaxis;
using WPF_Chemotaxis.Model;
using WPF_Chemotaxis.UX;
using System.Collections.Specialized;
using System.Windows.Media;

namespace WPF_Chemotaxis.VisualScripting
{
    internal class VSModelManager
    {
        private Dictionary<UIElement, ILinkable> visualToComponentMap = new();

        public VSModelManager()
        {
            Model.Model.Current.OnModelChanged += this.HandleModelChanges;
        }

        private void HandleModelChanges(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach(var item in e.NewItems)
                {
                    VSElementAttribute itemAttribute = item.GetType().GetCustomAttribute<VSElementAttribute>();
                    if (itemAttribute != null)
                    {
                        if (!visualToComponentMap.Values.Contains(item))
                        {

                        }
                    }
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dropped in e.OldItems)
                {
                    if (visualToComponentMap.Values.Contains(dropped))
                    {
                        List<UIElement> toRemove = new();
                        foreach (UIElement key in visualToComponentMap.Keys)
                        {
                            if (visualToComponentMap[key] == dropped)
                            {
                                toRemove.Add(key);
                            }
                        }
                        foreach(var elem in toRemove)
                        {
                            RemoveElement(elem);
                        }
                    }
                }
            }
        }

        private void RemoveElement(UIElement element)
        {
            visualToComponentMap.Remove(element);
            (VisualTreeHelper.GetParent(element) as Canvas).Children.Remove(element);
        }

        public bool AddNewModelPart(UIElement newVisual, VSViewModelElement vsToModelLinker)
        {
            if (visualToComponentMap.ContainsKey(newVisual)) return false;
            
            ILinkable newLink = Activator.CreateInstance(vsToModelLinker.TargetType) as ILinkable;
            newLink.Name = "New " + newLink.DisplayType;

            if (newLink != null)
            {
                visualToComponentMap.Add(newVisual, newLink);
                return true;
            }
            return false;
        }

        public bool TryGetModelElementFromVisual(UIElement visual, out ILinkable modelElement)
        {
            return visualToComponentMap.TryGetValue(visual, out modelElement);
        }

        public bool TryConnectElements(UIElement parentVisual, UIElement childVisual)
        {
            ILinkable parent, child;

            if(TryGetModelElementFromVisual(parentVisual, out parent) && TryGetModelElementFromVisual(childVisual, out child))
            {
                System.Diagnostics.Debug.Print(String.Format("parent {0} and child {1} both in model", parent.Name, child.Name));
                return FindConnectionMethod(parent, child);
            }
            return false;
        }

        public void TryDisconnectElements(UIElement parentVisual, UIElement childVisual)
        {
            ILinkable parent, child;

            if (TryGetModelElementFromVisual(parentVisual, out parent) && TryGetModelElementFromVisual(childVisual, out child))
            {
                parent.RemoveElement(child);
            }
        }

        private bool FindConnectionMethod(ILinkable parentLink, ILinkable childLink)
        {
            IEnumerable<MethodInfo> methods = parentLink.GetType().GetMethods().Where(method => method.GetCustomAttributes<ElementAdder>().Any());
            MethodInfo method = null;

            foreach (MethodInfo m in methods)
            {
                ElementAdder adder = (m.GetCustomAttribute<ElementAdder>() as ElementAdder);
                System.Diagnostics.Debug.Print(String.Format("parent has adder for type {0}",adder.type));
                if (adder.type == childLink.GetType())
                {
                    method = m;
                    break;
                }
            }
            if (method == null) return false;
            else
            {
                System.Diagnostics.Debug.Print(String.Format("Method found to add child of type {0}", childLink.GetType()));
                method.Invoke(parentLink, new object[] { childLink });
                return true;
            }
        }
    }
}
