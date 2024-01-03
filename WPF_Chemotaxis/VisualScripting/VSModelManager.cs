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
using System.Windows.Forms.Design;
using static System.Windows.Forms.LinkLabel;
using System.Diagnostics;

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
            foreach (var uiobj in ui_model_multimap.MultipleItemsList())
            {
                uiobj.Clean();
            }
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
                ILinkable newModelElement = (ILinkable) Activator.CreateInstance(fromMenu.TargetType);
                
                newModelElement.Name = "New " + newModelElement.DisplayType;
                if (newModelElement != null)
                {
                    VSUIElement newElement = new VSUIElement(fromMenu, clickPsn, newModelElement, targetCanvas);
                    ui_model_multimap.TryAdd(newElement, newModelElement);
                    var recovered = new List<VSDiagramObject>();
                    if(ui_model_multimap.TryGetValues(newModelElement, out recovered))
                    {
                    
                    }
                    DockableAttribute dockAttribute = newModelElement.GetType().GetCustomAttribute<DockableAttribute>();
                    LineConnectorAttribute lineAttribute = newModelElement.GetType().GetCustomAttribute<LineConnectorAttribute>();

                    if (dockAttribute != null)
                    {
                        TryAddDockedRelationship(newModelElement, dockAttribute);
                    }

                    if (lineAttribute != null)
                    {
                        
                        TryAddLineRelationship(newModelElement, lineAttribute);
                    }

                }
            }
            catch(TargetInvocationException e)
            {
                Trace.WriteLine(string.Format("failed to make instance of type {0} due to error {1}\n {2}", fromMenu.TargetType.Name, e.InnerException, e.StackTrace));
            }
            _islistening = true;
        }

        private void AddDetectedMainElement(ILinkable element)
        {
            //If an item that could be picked from the menu isn't listed in the visual componenet dictionary, make it and place it.
            if (ui_model_multimap!=null && !ui_model_multimap.Contains(element))
            {
                //Create model part programatically here
                Point newPoint = new Point(20 + Math.Max(600, targetCanvas.ActualWidth-40) * rnd.NextDouble(), 20 + Math.Max(500, targetCanvas.ActualHeight-40) * rnd.NextDouble());
                VSDiagramObject createdUIElement;
                if (factory.TryCreateUIForExtantModelElement(element, newPoint, out createdUIElement))
                {
                    ui_model_multimap.TryAdd(createdUIElement, element);
                }
            }
        }



        public List<VSRelationElement> GetConnections(VSDiagramObject handle)
        {
            var connections = (from sel in ui_model_multimap.MultipleItemsList().OfType<VSRelationElement>() where sel.HasHandle(handle) select sel);
            return connections.ToList();
        }

        private bool TryAddDockedRelationship(ILinkable dockableLink, DockableAttribute dock)
        {
            Type linkType = dockableLink.GetType();
            bool separateDock = true;
            ILinkable parent, child;
            parent = linkType.GetProperty(dock.parentPropertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(dockableLink) as ILinkable;

            if (dock.childPropertyName==null || dock.childPropertyName == "")
            {
                separateDock = false; // Keep track of whether the dock needs recording as its own model object.
                child = dockableLink; // Assume that dockable link is the dockable element if it doesn't specify.
            }
            else 
            {
                child = linkType.GetProperty(dock.childPropertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(dockableLink) as ILinkable;
            }
            if (parent == null || child == null) return false;

            Trace.WriteLine(string.Format("Making dockable connection for {0} and {1}", parent.Name, child.Name));

            List<VSDiagramObject> parentUISet, childUISet;     
            //If we have a parent, a child and UIs for both...
            if (ui_model_multimap.TryGetValues(parent, out parentUISet) && ui_model_multimap.TryGetValues(child, out childUISet)){
                //We check that, for every parentUI, we have a version of the child. 
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
                        //We have a parent without a child. If there's a child UI that's NOT docked, dock it to the parent.
                        foreach (var childUI in childUISet)
                        {
                            if (!childUI.Docked)
                            {
                                childUI.DockToVSObject(parentUI, dock.dockDistance);
                                attached = true;
                                break;
                            }
                        }
                    }
                    //If there's still no child UI, make a copy and dock it.
                    if (!attached)
                    {
                        var copy = childUISet[0].Duplicate();
                        copy.DockToVSObject(parentUI, dock.dockDistance);
                        //if(separateDock) ui_model_multimap.TryAdd(copy, (copy as VSUIElement).LinkedModelPart);
                        attached = true;
                    }    
                }
            }
            return false;
        }

        private bool TryAddLineRelationship(ILinkable lineConnectorLink, LineConnectorAttribute cnx)
        {
            Trace.WriteLine(string.Format("Adding lineconnector relationship for {0}", lineConnectorLink.Name));
            Type linkType = lineConnectorLink.GetType();
            bool isSeparateModelPart = true;
            ILinkable parent;
            if(cnx.parentPropertyName==null || cnx.parentPropertyName == "")
            {
                isSeparateModelPart = false; // Keep track of whether the dock needs recording as its own model object.
                parent = lineConnectorLink; // Assume that dockable link is the dockable element if it doesn't specify.
            }
            else parent = linkType.GetProperty(cnx.parentPropertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(lineConnectorLink) as ILinkable;
            if (parent == null) return false;
            Trace.WriteLine(string.Format("Line has parent {0}", parent.Name));
            List<VSDiagramObject> parentDuplicates;
            if (ui_model_multimap.TryGetValues(parent, out parentDuplicates))
            {
                foreach (var par in parentDuplicates)
                {
                    new VSRelationElement(par, lineConnectorLink, isSeparateModelPart, targetCanvas);
                }
                targetCanvas.InvalidateVisual();
                return true;
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
                            &&    !element.GetType().IsAssignableTo(typeof(ExpressionCoupler))
                            &&    !element.GetType().IsAssignableTo(typeof(ICellComponent))
                            &&    element.GetType().GetCustomAttribute<LineConnectorAttribute>()==null
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

            //Logic components, or they don't attach things properly!
            foreach (ILinkable link in Model.Model.MasterElementList.Where(l=>l.GetType().IsAssignableTo(typeof(ICellComponent))))
            {
                if (ui_model_multimap.TryGetValues(link, out el))
                {

                }
                else
                {
                    TryAddNewILinkable(link);
                }
            }
            // Then docking couplers
            foreach (ExpressionCoupler coup in Model.Model.MasterElementList.OfType<ExpressionCoupler>())
            {
                if (ui_model_multimap.TryGetValues(coup, out el))
                {

                }
                else
                {
                    TryAddNewILinkable(coup);
                }
            }
            //Then line couplers
            var lineElements = from element in Model.Model.MasterElementList
                               where element.GetType().GetCustomAttribute<LineConnectorAttribute>() != null
                               select element;
            foreach (var line in lineElements)
            {
                if (ui_model_multimap.TryGetValues(line, out el))
                {

                }
                else
                {
                    TryAddNewILinkable(line);
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
                // Three attributes control ILinkable behaviour. It can, non-exclusively, be
                // A) A diagram element, with its own graphic.
                // B) dockable to something else
                // C) drawing a line to other things.
                VSElementAttribute     itemAttribute = item.GetType().GetCustomAttribute<VSElementAttribute>();
                DockableAttribute      dockAttribute = item.GetType().GetCustomAttribute<DockableAttribute>();
                LineConnectorAttribute lineAttribute = item.GetType().GetCustomAttribute<LineConnectorAttribute>();

                if (itemAttribute != null)
                {
                    AddDetectedMainElement(item);
                }

                if (dockAttribute != null)
                {
                    
                    TryAddDockedRelationship(item, dockAttribute);
                }

                if (lineAttribute != null)
                {
                    
                    TryAddLineRelationship(item, lineAttribute);
                }
                return true;
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
                        
                        //If the map contains it, remove it from the model
                        HashSet<VSDiagramObject> droppedUIs;
                        if (ui_model_multimap.Remove(dropped, out droppedUIs))
                        {

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
        public bool TryDeleteVisual(VSDiagramObject deletedVisual)
        {
            if (deletedVisual != null)
            {
                var iter = deletedVisual.Children.OfType<VSDiagramObject>().ToList();
                foreach (var child in iter)
                {
                    TryDeleteVisual(child);
                }
                var cnx = GetConnections(deletedVisual);
                foreach (var connection in cnx)
                {
                    TryDeleteVisual(connection);
                }
                if (deletedVisual.Docked)
                {
                    deletedVisual.Undock();
                }
                deletedVisual.Clean();
                RemoveElementFromVisualTree(deletedVisual);

                ILinkable link;
                ui_model_multimap.Remove(deletedVisual, out link);

                if (!ui_model_multimap.Contains(link))
                {
                    Model.Model.Current.RemoveElement(link);
                }
               
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
                if (childLink.GetType().IsAssignableTo(adder.type))
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
                Trace.WriteLine(String.Format("Method {1} found to add child of type {0} to parent {2}", childLink.GetType(), method.Name, parentLink.Name));
                method.Invoke(parentLink, new object[] { childLink });
                return true;
            }
        }

        public async void JitterOnLoad()
        {
            Debug.Print("JITTER");
            await Task.Delay(10);
            foreach (VSDiagramObject vsObj in ui_model_multimap.MultipleItemsList())
            {
                if(ui_model_multimap.TryGetValue(vsObj, out ILinkable link))
                {
                    Debug.Print(String.Format("Checking link {0}", link.Name));
                    if (link is CellType)
                    {
                        Debug.Print(String.Format("JITTER CELL {0}", link.Name));
                        vsObj.SetAbsolutePosition(new Point(vsObj.AbsolutePosition.X+1.0, vsObj.AbsolutePosition.Y));
                        vsObj.SetAbsolutePosition(new Point(vsObj.AbsolutePosition.X-1.0, vsObj.AbsolutePosition.Y));
                    }
                }
            }
        }

        // Click on item in visual model
        
    }
}
