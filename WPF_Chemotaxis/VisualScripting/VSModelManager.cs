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
using System.Windows.Input;
using System.Windows.Shapes;

namespace WPF_Chemotaxis.VisualScripting
{
    internal class VSModelManager
    {
        private Multimap<VSDiagramObject, ILinkable> ui_model_multimap = new();
        private Dictionary<VSDiagramObject, List<VSDiagramObject>> radial_child_elements = new();
        private Dictionary<VSDiagramObject, List<VSDiagramObject>> list_child_elements = new();
        private Dictionary<VSDiagramObject, List<VSDiagramObject>> arrow_child_elements = new();
        private Canvas targetCanvas;
        private VisualModelElementFactory factory;
        private VisualScriptingSelectionManager selectionManager;
        private SciRand rnd;
        private bool _islistening = true;

        private static VSModelManager _current;
        public static VSModelManager Current
        {
            get
            {
                if(_current == null)
                {
                    _current = new();
                }
                return _current;
            }
        }

        private VSModelManager()
        { 
            Model.Model.Current.OnModelChanged += this.HandleModelChanges;
            this.selectionManager = VisualScriptingSelectionManager.Current;
            this.rnd = new();
        }
        public void Init(Canvas targetCanvas)
        {
            if(this.targetCanvas==null) this.targetCanvas = targetCanvas;
            this.factory = new(targetCanvas);
        }

        /*
         * Creates a new ILinkable and then gets the factoy to make and link a UI to it.
         * 
         */
        public void CreateNewModelElementFromMenu(VSListMenuElement fromMenu, Point clickPsn)
        {
            _islistening = false;
            try
            {
                System.Diagnostics.Debug.Print(string.Format("About to create object of type {0}, but with listening={0}", fromMenu.TargetType.Name, _islistening));
                ILinkable newModelElement = (ILinkable) Activator.CreateInstance(fromMenu.TargetType);
                System.Diagnostics.Debug.Print("Created object of type " + newModelElement.GetType().Name);
                newModelElement.Name = "New " + newModelElement.DisplayType;
                if (newModelElement != null)
                {
                    System.Diagnostics.Debug.Print("Making UI element... " + fromMenu.TargetType.Name);
                    VSUIElement newElement = new VSUIElement(fromMenu, clickPsn, newModelElement, targetCanvas);
                    ui_model_multimap.TryAdd(newElement, newModelElement);
                }
            }
            catch(TargetInvocationException e)
            {
                System.Diagnostics.Debug.Print(string.Format("failed to make instance of type {0} due to error {1}\n {2}", fromMenu.TargetType.Name, e.InnerException, e.StackTrace));
            }
            _islistening = true;
        }

        private void AddDetectedMainElement(ILinkable element)
        {
            //If an item that could be picked from the menu isn't listed in the visual componenet dictionary, make it and place it.
            if (!ui_model_multimap.Contains(element))
            {
                //Create model part programatically here
                Point newPoint = new Point(10 + (targetCanvas.ActualWidth-20) * rnd.NextDouble(), 10 + (targetCanvas.ActualHeight-20) * rnd.NextDouble());
                VSDiagramObject createdUIElement;
                System.Diagnostics.Debug.Print(String.Format("Trying to create UI element for new {0}", element.DisplayType));
                if (factory.TryCreateUIForExtantModelElement(element, newPoint, out createdUIElement))
                {
                    System.Diagnostics.Debug.Print(String.Format("Trying to add to dictionary", element.DisplayType));
                    ui_model_multimap.TryAdd(createdUIElement, element);
                    System.Diagnostics.Debug.Print(String.Format("Added!"));
                }
            }
        }

        private void AddChildLineUI(VSDiagramObject parent, VSDiagramObject child, ILinkable relationLink)
        {

            VSRelationElement relation = new VSRelationElement(parent, child, relationLink, targetCanvas);
            ui_model_multimap.TryAdd(relation, relation.ModelReation);
            
            targetCanvas.InvalidateVisual();
        }


        private void AddUIChildToUIParent(VSDiagramObject parent, VSDiagramObject child, VSRelationAttribute relationParams, ILinkable relationalModelLink)
        {
            switch(relationParams.forcedPositionType)
            {
                case ForcedPositionType.NONE:

                    AddChildLineUI(child, parent, relationalModelLink);
                    List<VSDiagramObject> lineChildren;
                    
                    /*if (radial_child_elements.TryGetValue(parent, out lineChildren))
                    {
                        lineChildren.Add(child);
                    }
                    else
                    {
                        lineChildren = new List<VSDiagramObject>() { child };
                        radial_child_elements.Add(parent, lineChildren);
                    }*/

                    break;
                case ForcedPositionType.RADIUS:

                    child.DockToVSObject(parent, relationParams.forcePositionDistance, relationalModelLink); 

                    /*
                    List<VSDiagramObject> radialChildren;
                    if(radial_child_elements.TryGetValue(parent, out radialChildren)){
                        radialChildren.Add(child);
                    }
                    else
                    {
                        radialChildren = new List<VSDiagramObject>() {child};
                        radial_child_elements.Add(parent, radialChildren);
                    }*/

                    break;

                case ForcedPositionType.LIST:
                    break;
                case ForcedPositionType.WORKERCLASS:
                    break;

            }
        }

        public List<VSRelationElement> GetConnections(VSDiagramObject handle)
        {
            var connections = (from sel in ui_model_multimap.MultipleItemsList().OfType<VSRelationElement>() where sel.HasHandle(handle) select sel);
            return connections.ToList();
        }

        private bool TryAddRelationshipMarker(ILinkable relationalLink, VSRelationAttribute relation)
        {
       
            Type linkType = relationalLink.GetType();
            ILinkable parent = linkType.GetField(relation.parentFieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(relationalLink) as ILinkable;
            ILinkable child  = linkType.GetField(relation.childFieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(relationalLink) as ILinkable;

            if(parent!=null && child != null)
            {
                System.Diagnostics.Debug.Print(String.Format("Found parent {0} and child {1} objects", parent.Name, child.Name));
                List<VSDiagramObject> parentUISet, childUISet;
                
                if(ui_model_multimap.TryGetValues(parent, out parentUISet) && ui_model_multimap.TryGetValues(child, out childUISet)){
                    System.Diagnostics.Debug.Print(String.Format("Fetched UI elements for the ends of the relations...", parent.Name, child.Name));


                    // SO HERE IS A PROBLEM! WE ARE ADDING RELATIONSHIPS BASED ON A LISTENER- WE NEED THE NEW RELATION LINK UI THAT WAS GENERATED BUT DON'T KNOW WHICH- HOWEVER WE DO KNOW THAT IT'S UNDOCKED!
                    // AN UNDOCKED COPY IS EITHER THE ONLY ONE, OR IT IS NEW, SO WE FIND THE UNDOCKED ONE AND WE PASS IN THAT!

                    //However- none of this applies to lines, so we might have to work around that here...

                    foreach(var childUI in childUISet)
                    {
                        if (!childUI.Docked)
                        {
                            AddUIChildToUIParent(parentUISet[0], childUI, relation, relationalLink);
                            return true;
                        }
                    }
                    AddUIChildToUIParent(parentUISet[0], childUISet[0], relation, relationalLink);
                    return true;
                }
            }
            return false;
        }

        private void HandleModelChanges(object sender, NotifyCollectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.Print(string.Format("Detected model change with listening={0}", _islistening));
            //If we are adding this ourselves, don't also react to it.
            if (!_islistening)
            {
                return;
            }
            if(e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach(var uncastitem in e.NewItems)
                {
                    ILinkable item = uncastitem as ILinkable;
                    if (item != null)
                    {
                        // If an element that can be added via menu has been added elsewhere...
                        VSElementAttribute itemAttribute = item.GetType().GetCustomAttribute<VSElementAttribute>();
                        if (itemAttribute != null)
                        {
                            System.Diagnostics.Debug.Print("Found menu item attribute!");
                            AddDetectedMainElement(item);
                        }
                        else
                        {
                            VSRelationAttribute relationAttribute = item.GetType().GetCustomAttribute<VSRelationAttribute>();
                            if (relationAttribute != null)
                            {
                                System.Diagnostics.Debug.Print("Found relation attribute!");
                                TryAddRelationshipMarker(item, relationAttribute);
                            }
                        }
                    }
                }
            }

            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var uncastdropped in e.OldItems)
                {
                    //Foreach dropped item
                    ILinkable dropped =  uncastdropped as ILinkable;
                    if (dropped != null)
                    {
                        System.Diagnostics.Debug.Print("Caught lost item from observablelist");
                        //If the map contains it, remove it from the model
                        HashSet<VSDiagramObject> droppedUIs;
                        if (ui_model_multimap.Remove(dropped, out droppedUIs))
                        {
                            System.Diagnostics.Debug.Print("Remove from model map returned true");
                            //Then iterate and remove UIs from visual tree
                            foreach (VSDiagramObject item in droppedUIs)
                            {
                                RemoveElementFromVisualTree(item);
                            }
                        }
                    }
                }
            }
        }

        //Lets listeners to model change handle the visual side! Does remove ALL parts of an element though, not just selected one.
        /*public bool TryDeleteModelElement(VSDiagramObject byDiagramObject)
        {
            var targetObject = byDiagramObject as VSUIElement;
            if (targetObject!=null)
            {
                Model.Model.Current.RemoveElement(targetObject.LinkedModelPart);
                return true;
            }
            return false;
        }*/
        public bool TryDeleteVisual(VSDiagramObject byDiagramObject)
        {
            //var targetObject = byDiagramObject as VSUIElement;
            if (byDiagramObject != null)
            {
                ILinkable link;
                ui_model_multimap.Remove(byDiagramObject, out link);

                if (!ui_model_multimap.Contains(link))
                {
                    Model.Model.Current.RemoveElement(link);
                }
                RemoveElementFromVisualTree(byDiagramObject);
                return true;
            }
            return false;
        }

        private void RemoveElementFromVisualTree(VSDiagramObject element)
        {
            element.Dispose();
            
        }

        public bool TryAdd(VSDiagramObject visual, ILinkable link)
        {
            return ui_model_multimap.TryAdd(visual, link);
        }

        public bool TryGetUIListFromLink(ILinkable link, out List<VSDiagramObject> uis)
        {
            return ui_model_multimap.TryGetValues(link, out uis);
        }

        public bool TryGetModelElementFromVisual(VSDiagramObject visual, out ILinkable modelElement)
        {
            return ui_model_multimap.TryGetValue(visual, out modelElement);
        }

        public bool TryConnectElements(VSUIElement parentVisual, VSUIElement childVisual)
        {
            return FindConnectionMethod(parentVisual.LinkedModelPart, childVisual.LinkedModelPart);
        }

        public void TryDisconnectElements(VSDiagramObject parentVisual, VSDiagramObject childVisual)
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

        // Click on item in visual model
        
    }
}
