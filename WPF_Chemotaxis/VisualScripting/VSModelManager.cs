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
        private Multimap<UIElement, ILinkable> ui_model_multimap = new();
        private Dictionary<UIElement, List<UIElement>> radial_child_elements = new();
        private Dictionary<UIElement, List<UIElement>> list_child_elements = new();
        private Dictionary<UIElement, List<UIElement>> arrow_child_elements = new();
        private Canvas targetCanvas;
        private VisualModelElementFactory factory;
        private VisualScriptingSelectionManager selectionManager;
        private SciRand rnd;
        private bool _islistening = true;

        public VSModelManager(Canvas targetCanvas, VisualScriptingSelectionManager selectionManager)
        {
            Model.Model.Current.OnModelChanged += this.HandleModelChanges;
            this.targetCanvas = targetCanvas;
            this.factory = new(targetCanvas);
            this.selectionManager = selectionManager;
            this.rnd = new();
        }

        /*
         * Creates a new ILinkable and then gets the factoy to make and link a UI to it.
         * 
         */
        public void CreateNewModelElementFromMenu(VSListMenuElement fromMenu, Point clickPsn)
        {
            _islistening = false;

            ILinkable newModelElement = Activator.CreateInstance(fromMenu.TargetType) as ILinkable;
            newModelElement.Name = "New " + newModelElement.DisplayType;
            if (newModelElement != null)
            {
                VSUIElement newElement = new VSUIElement(fromMenu, clickPsn, newModelElement, targetCanvas, DefaultLeftMouseDown, DefaultLeftMouseUp, DefaultRightMouseUp);
                ui_model_multimap.TryAdd(newElement, newModelElement);
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
                if (factory.TryCreateUIForExtantModelElement(element, newPoint, out createdUIElement, DefaultLeftMouseDown, DefaultLeftMouseUp, DefaultRightMouseUp))
                {
                    ui_model_multimap.TryAdd(createdUIElement, element);
                }
            }
        }

        private void AddRadialDockedUI(UIElement parent, UIElement child, double dist)
        {
            Canvas parentCanvas = VisualTreeHelper.GetParent(parent) as Canvas;
            Canvas childCanvas = VisualTreeHelper.GetParent(child) as Canvas;
            //Calculate point
            Point childAbsPosition = child.TransformToAncestor(targetCanvas).Transform(new Point(0.5 * child.RenderSize.Width, 0.5 * child.RenderSize.Height));
            Point parentAbsPosition = parent.TransformToAncestor(targetCanvas).Transform(new Point(0.5 * parent.RenderSize.Width, 0.5 * parent.RenderSize.Height));

            Point currentRelativePosition = new Point(childAbsPosition.X - parentAbsPosition.X, childAbsPosition.Y - parentAbsPosition.Y);
            double targetAngleRadians = Math.Atan2(currentRelativePosition.Y, currentRelativePosition.X);
            double targetRotationDegrees = 90d + 180d * targetAngleRadians / Math.PI;
            //double multiplier = relationParams.forcePositionDistance / Math.Sqrt(currentRelativePosition.X * currentRelativePosition.X + currentRelativePosition.Y * currentRelativePosition.Y);

            //Reparent
            Canvas oldParent = (childCanvas.Parent as Canvas);
            if (oldParent != null)
            {
                oldParent.Children.Remove(childCanvas);
            }
            parentCanvas.Children.Add(childCanvas);

            Point targetPoint = new Point(dist * Math.Cos(targetAngleRadians), dist * Math.Sin(targetAngleRadians));

            //Update to new point and rotation
            Canvas.SetLeft(childCanvas, targetPoint.X);
            Canvas.SetTop(childCanvas, targetPoint.Y);
            childCanvas.RenderTransform = new RotateTransform(angle: targetRotationDegrees);

            foreach (var iter in childCanvas.Children)
            {
                TextBox label = iter as TextBox;
                if (label != null)
                {
                    label.RenderTransform = (Transform)childCanvas.RenderTransform.Inverse;
                }
            }
            targetCanvas.InvalidateVisual();
        }

        private void AddChildLineUI(UIElement parent, UIElement child)
        {
            Canvas parentCanvas = VisualTreeHelper.GetParent(parent) as Canvas;
            Canvas childCanvas = VisualTreeHelper.GetParent(child) as Canvas;

            Line line = new Line();
            line.Stroke = Brushes.Blue;
            line.StrokeThickness = 4;

            Point p1 = child.TransformToAncestor(targetCanvas).Transform(new Point(0.5*child.RenderSize.Width, 0.5*child.RenderSize.Height)); 
            Point p2 = parent.TransformToAncestor(targetCanvas).Transform(new Point(0.5*parent.RenderSize.Width, 0.5*parent.RenderSize.Height));

            line.X1 = p1.X;
            line.Y1 = p1.Y;
            line.X2 = p2.X;
            line.Y2 = p2.Y;
            targetCanvas.Children.Add(line);
            Canvas.SetZIndex(line, -1);


            targetCanvas.InvalidateVisual();
        }


        private void AddUIChildToUIParent(UIElement parent, UIElement child, VSRelationAttribute relationParams)
        {
            

            switch(relationParams.forcedPositionType)
            {
                case ForcedPositionType.NONE:

                    AddChildLineUI(child, parent);
                    List<UIElement> lineChildren;
                    if (radial_child_elements.TryGetValue(parent, out lineChildren))
                    {
                        lineChildren.Add(child);
                    }
                    else
                    {
                        lineChildren = new List<UIElement>() { child };
                        radial_child_elements.Add(parent, lineChildren);
                    }

                    break;
                case ForcedPositionType.RADIUS:

                    AddRadialDockedUI(parent, child, relationParams.forcePositionDistance);
                    List<UIElement> radialChildren;
                    if(radial_child_elements.TryGetValue(parent, out radialChildren)){
                        radialChildren.Add(child);
                    }
                    else
                    {
                        radialChildren = new List<UIElement>() {child};
                        radial_child_elements.Add(parent, radialChildren);
                    }

                    break;

                case ForcedPositionType.LIST:
                    break;
                case ForcedPositionType.WORKERCLASS:
                    break;

            }
        }

        private bool TryAddRelationshipMarker(ILinkable relationalLink, VSRelationAttribute relation)
        {
       
            Type linkType = relationalLink.GetType();
            ILinkable parent = linkType.GetField(relation.parentFieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(relationalLink) as ILinkable;
            ILinkable child  = linkType.GetField(relation.childFieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(relationalLink) as ILinkable;

            if(parent!=null && child != null)
            {
                System.Diagnostics.Debug.Print(String.Format("Found parent {0} and child {1} objects", parent.Name, child.Name));
                List<UIElement> parentUISet, childUISet;
                
                if(ui_model_multimap.TryGetValues(parent, out parentUISet) && ui_model_multimap.TryGetValues(child, out childUISet)){

                    AddUIChildToUIParent(parentUISet[0], childUISet[0], relation);
                }
            }
            return false;
        }

        private void HandleModelChanges(object sender, NotifyCollectionChangedEventArgs e)
        {
            //If we are adding this ourselves, don't also react to it.
            if (!_islistening) return;

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
                        //If the map contains it, remove it from the model
                        HashSet<UIElement> droppedUIs;
                        if (ui_model_multimap.Remove(dropped, out droppedUIs))
                        {
                            //Then iterate and remove UIs from visual tree
                            foreach (UIElement item in droppedUIs)
                            {
                                RemoveElementFromVisualTree(item);
                            }
                        }
                    }
                }
            }
        }

        private void RemoveElementFromVisualTree(UIElement element)
        {
            (VisualTreeHelper.GetParent(element) as Canvas).Children.Remove(element);
        }

        public bool TryGetModelElementFromVisual(UIElement visual, out ILinkable modelElement)
        {
            return ui_model_multimap.TryGetValue(visual, out modelElement);
        }

        public bool TryConnectElements(UIElement parentVisual, UIElement childVisual)
        {
            ILinkable parent, child;

            if(TryGetModelElementFromVisual(parentVisual, out parent) && TryGetModelElementFromVisual(childVisual, out child))
            {
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

        // Click on item in visual model
        
    }
}
