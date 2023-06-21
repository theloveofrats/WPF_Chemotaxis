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
using System.Xml.Linq;

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
            this.rnd = new();
        }
        public void Init(Canvas targetCanvas)
        {
            if(this.targetCanvas==null) this.targetCanvas = targetCanvas;
            this.factory = new(targetCanvas);
            Model.Model.Current.ModelChanged += this.HandleModelChanges;
            ParseOnLoad();
        }

        public void Clear()
        {
            ui_model_multimap?.Clear();
            targetCanvas?.Children.Clear();
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
            System.Diagnostics.Debug.Print(String.Format("Adding element {0}", element.Name));
            //If an item that could be picked from the menu isn't listed in the visual componenet dictionary, make it and place it.
            if (ui_model_multimap!=null && !ui_model_multimap.Contains(element))
            {
                if(targetCanvas==null) System.Diagnostics.Debug.Print(String.Format("NO CANVAS SOMEHOW"));
                //Create model part programatically here
                Point newPoint = new Point(20 + Math.Max(600, targetCanvas.ActualWidth-40) * rnd.NextDouble(), 20 + Math.Max(500, targetCanvas.ActualHeight-40) * rnd.NextDouble());
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

        private void AddChildLineUI(ILinkable parentModelElement, ILinkable childModelElement, ILinkable relationLink)
        {
            //If there are duplicates to make, we need to know
            
            List<VSDiagramObject> parentDuplicates, childDuplicates;
            if (ui_model_multimap.TryGetValues(parentModelElement, out parentDuplicates) && ui_model_multimap.TryGetValues(childModelElement, out childDuplicates)){
                System.Diagnostics.Debug.Print(String.Format("Both {0} and {1} have UI elements in the database", parentModelElement.Name, childModelElement.Name));
                foreach (var par in parentDuplicates)
                {
                    VSRelationElement relation = new VSRelationElement(par, relationLink, targetCanvas);
                    ui_model_multimap.TryAdd(relation, relation.ModelReation);
                }
                targetCanvas.InvalidateVisual();
            }
        }


        private void AddUIChildToUIParent(VSDiagramObject parent, VSDiagramObject child, VSRelationAttribute relationParams, ILinkable relationalModelLink)
        {
            switch(relationParams.forcedPositionType)
            {
                case ForcedPositionType.NONE:

              

                    break;
                case ForcedPositionType.RADIUS:

                    child.DockToVSObject(parent, relationParams.forcePositionDistance, relationalModelLink); 

                 

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
            ILinkable parent;
            if (relation.parentFieldName == null)
            {
                parent = relationalLink;
            }
            else {
                parent = linkType.GetField(relation.parentFieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(relationalLink) as ILinkable;
            }
            ILinkable child  = linkType.GetField(relation.childFieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(relationalLink) as ILinkable;

            // If we have both model links.
            if (parent!=null && child != null)
            {
                List<VSDiagramObject> parentUISet, childUISet;
                System.Diagnostics.Debug.Print(String.Format("Adding relationship marker {0} for child {1} and parent {2}", relationalLink.Name, child.Name, parent.Name));

                //And we have at least one visual element for each...
                if (ui_model_multimap.TryGetValues(parent, out parentUISet) && ui_model_multimap.TryGetValues(child, out childUISet)){
                    if (relation.forcedPositionType != ForcedPositionType.NONE)
                    {
                        //We check thsat, for every parentUI, we have a versio of the child. 
                        foreach (var parentUI in parentUISet)
                        {
                            bool attached = false;
                            foreach (var childUI in childUISet)
                            {
                                //If the parentUI contains this child, it's a docked child, so tick off this parent from the list
                                if (parentUI.Children.Contains(childUI))
                                {
                                    attached = true;
                                    break;
                                }
                            }
                            if (!attached)
                            {
                                foreach (var childUI in childUISet)
                                {
                                    if (!childUI.Docked)
                                    {
                                        AddUIChildToUIParent(parentUI, childUI, relation, relationalLink);
                                        attached = true;
                                        break;
                                    }
                                }
                                if (!attached)
                                {

                                    //Or, add a copy I guess?
                                    System.Diagnostics.Debug.Print(String.Format("COPY {0} TO ADD TO PARENT {1}", child.Name, parent.Name));
                                    var copy = childUISet[0].Duplicate();
                                    AddUIChildToUIParent(parentUI, copy, relation, relationalLink);
                                    TryAdd(copy, (copy as VSUIElement).LinkedModelPart);
                                    attached = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.Print(String.Format("No forced position..."));

                        AddChildLineUI(parent, child, relationalLink);
                        List<VSDiagramObject> lineChildren;
                        return true;
                    }
                }
            }
            return false;
        }

        private void ParseOnLoad()
        {
            List<VSDiagramObject> el;
            foreach (CellType newCellType in Model.Model.MasterElementList.OfType<CellType>())
            {
                if(ui_model_multimap!=null && ui_model_multimap.TryGetValues(newCellType, out el))
                {
                    // If it already has a representative, don't add it!

                }
                else
                {
                    TryAddNewILinkable(newCellType);
                }
            }
            foreach(Receptor newReceptor in Model.Model.MasterElementList.OfType<Receptor>())
            {
                if (ui_model_multimap.TryGetValues(newReceptor, out el))
                {
                    
                }
                else
                {
                    TryAddNewILinkable(newReceptor);
                }
            }
            foreach (Ligand newLigand in Model.Model.MasterElementList.OfType<Ligand>())
            {
                if (ui_model_multimap.TryGetValues(newLigand, out el))
                {

                }
                else
                {
                    TryAddNewILinkable(newLigand);
                }
            }

            var remaining = from element in Model.Model.MasterElementList 
                            where !element.GetType().IsAssignableTo(typeof(CellType))
                            &&    !element.GetType().IsAssignableTo(typeof(Receptor))
                            &&    !element.GetType().IsAssignableTo(typeof(Ligand))
                            select element;

            foreach (var link in remaining)
            {
                if (ui_model_multimap.TryGetValues(link, out el))
                {

                }
                else
                {
                    TryAddNewILinkable(link);
                }
            }

            AutoarrangeChildren();
        }


        private async Task AutoarrangeChildren()
        {
            await Task.Delay(100);

            var modelElements = targetCanvas.Children.OfType<VSUIElement>();
            
            var cellUIs = (from element in modelElements 
                          where element.LinkedModelPart.GetType()==typeof(CellType) 
                          select element).ToList();

            var ligandUIs = (from element in modelElements
                           where element.LinkedModelPart.GetType() == typeof(Ligand)
                           select element).ToList();

            var otherUIs = modelElements.Except(cellUIs.Union(ligandUIs)).ToList();

            double iWidth =  Math.Max(targetCanvas.ActualWidth, 1050);
            double iHeight = Math.Max(targetCanvas.ActualHeight, 600);

            double rows = 0;
            if (otherUIs.Count > 0) rows+=0.5;
            if (cellUIs.Count > 0) rows+=2;
            if (ligandUIs.Count > 0) rows+=0.5;

            double hStep = iHeight /(rows+1);
            double row = 0;

            if (otherUIs.Count > 0) row += 0.5;
            for (int i = 1; i <= otherUIs.Count; i++)
            {
                var otherUI = otherUIs[i-1];
                otherUI.SetPosition(i * iWidth / (otherUIs.Count + 1), row*hStep);
            }

            if (cellUIs.Count > 0) row += 1;
            for (int i = 1; i <= cellUIs.Count; i++)
            {
                var cellUI = cellUIs[i-1];
                cellUI.SetPosition(i * iWidth / (cellUIs.Count + 1), row * hStep);
            }
            if (cellUIs.Count > 0) row += 1;

            if (ligandUIs.Count > 0) row += 0.5;
            for (int i = 1; i <= ligandUIs.Count; i++)
            {
                var ligandUI = ligandUIs[i-1];
                ligandUI.SetPosition(i * iWidth / (ligandUIs.Count + 1), row * hStep);
            }
        }

        private bool TryAddNewILinkable(ILinkable item)
        {
            if (item != null)
            {
                // If an element that can be added via menu has been added elsewhere...
                VSElementAttribute itemAttribute = item.GetType().GetCustomAttribute<VSElementAttribute>();
                if (itemAttribute != null)
                {
                    AddDetectedMainElement(item);
                    return true;
                }
                VSRelationAttribute relationAttribute = item.GetType().GetCustomAttribute<VSRelationAttribute>();
                if (relationAttribute != null)
                {
                    System.Diagnostics.Debug.Print(String.Format("Found relation attribute of type {0}", item.Name));
                    TryAddRelationshipMarker(item, relationAttribute);
                    return true;
                }
            }
            return false;
        }

        private void HandleModelChanges(object sender, NotifyCollectionChangedEventArgs e)
        {
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
                    TryAddNewILinkable(item);
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
                var iter = byDiagramObject.Children.OfType<VSDiagramObject>().ToList();
                foreach (var child in iter)
                {
                    TryDeleteVisual(child);
                }
                var cnx = GetConnections(byDiagramObject);
                foreach (var connection in cnx)
                {
                    TryDeleteVisual(connection);
                }

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
            if (method == null)
            {
                return childLink.TryAddTo(parentLink);
            }
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
