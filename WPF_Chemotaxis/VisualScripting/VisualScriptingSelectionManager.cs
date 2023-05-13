using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace WPF_Chemotaxis.VisualScripting
{
    internal class VisualScriptingSelectionManager : INotifyPropertyChanged
    {
        private const double BREAK_DOCK_SQ_DIST = 250 * 250;

        private Canvas baseCanvas;
        private Point dragFrom;
        private Point dragTo; 
        private Point dragOffset;
        private DragAdorner dragGhost;
        private bool preDrag = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public static VisualScriptingSelectionManager Current { get; private set; }

        public bool IsDragging { get; private set; }

        public VSDiagramObject SelectedElement { get; private set; }

        private VSListMenuElement selectedMenuItem;
        public VSListMenuElement SelectedMenuItem
        {
            get
            {
                return selectedMenuItem;
            }
            private set
            {
                selectedMenuItem = value;
                NotifyPropertyChanged("SelectedMenuItem");
            }
        }

        private VisualScriptingSelectionManager(Canvas baseCanvas)
        {
            this.baseCanvas = baseCanvas;
            Current = this;
        }

        public static void InitialiseVisualScriptingSelectionManager(Canvas baseCanvas)
        {
            new VisualScriptingSelectionManager(baseCanvas);
        }

        public bool HasSelection
        {
            get
            {
                return SelectedElement != null;
            }
        }
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public void SelectElement(VSDiagramObject element)
        {
            if (element == null) return;
            SelectedElement = element;
            SelectedElement.Effect = new DropShadowEffect()
            {
                BlurRadius = 20,
                Color = Color.FromRgb(180, 180, 0),
                ShadowDepth = 0
            };
        }
        public void ClearSelection()
        {
            if (SelectedElement != null)
            {
                SelectedElement.Effect = null;
                SelectedElement = null;
            }
        }
        public void SelectMenuItem(VSListMenuElement newItem)
        {
            SelectedMenuItem = newItem;
        }
        public void ClearMenuSelection()
        {
            SelectedMenuItem = null;
        }

        private void UnrotateTextLabels(Canvas parentObject, RotateTransform toUndo)
        {
            foreach (var child in parentObject.Children)
            {
                TextBox label = child as TextBox;
                if (label != null)
                {
                    label.RenderTransform = (Transform)toUndo.Inverse;
                }
                else
                {
                    Canvas childCanvas = child as Canvas;
                    if (childCanvas != null)
                    {
                        UnrotateTextLabels(childCanvas, toUndo);
                    }
                }
            }
        }

        public void RotateSelected(double degrees)
        {

            RotateTransform current = SelectedElement?.RenderTransform as RotateTransform;

            if (current == null)
            {
                SelectedElement.RenderTransform = new RotateTransform();
                current = SelectedElement.RenderTransform as RotateTransform;
            }
            current.Angle += degrees;

            UnrotateTextLabels(SelectedElement, current);
        }

        // Point clickPsn is always relative to the base canvas!
        private void AddDragGhost(Point clickPsn)
        {
            if (HasSelection)
            {
                UIElement parent = VisualTreeHelper.GetParent(SelectedElement) as UIElement;
                dragOffset = baseCanvas.TransformToDescendant(SelectedElement).Transform(dragFrom);

                dragGhost = new DragAdorner(parent, SelectedElement,dragOffset);
                dragGhost.IsHitTestVisible = false;
                AdornerLayer.GetAdornerLayer(parent).Add(dragGhost);
            }
        }

        public void MoveSelected(Point moveTo)
        {
            if (!HasSelection) return;
            double rotation;
            bool breakDock;
            Point newPsn = GetPredictedDragPosition(moveTo, out rotation, out breakDock);
            
            if (SelectedElement.Docked)
            {
                if (!breakDock)
                {
                    SelectedElement.SetPosition(newPsn.X - dragOffset.X, newPsn.Y - dragOffset.Y);
                    SelectedElement.SetToDockedPosition();
                    return;
                }
                else
                {
                    SelectedElement.SetPosition(newPsn.X - dragOffset.X+SelectedElement.DockedTo.Position.X, newPsn.Y - dragOffset.Y+SelectedElement.DockedTo.Position.Y);
                    SelectedElement.Undock();
                    return;
                }
            }
            else
            {
                SelectedElement.SetPosition(newPsn.X - dragOffset.X, newPsn.Y - dragOffset.Y);
            }
        }

        //Does not actually start a drag unless UpdateDrag notices an appropriate distance!
        public void StartDrag(Point startPos)
        {
            dragFrom = startPos;
            preDrag = true;
        }

        public void UpdateDrag(Point newPos)
        {
            dragTo = newPos;
            if (!IsDragging && preDrag)
            {
                if (Math.Abs(dragTo.X - dragFrom.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(dragTo.Y - dragFrom.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    dragFrom = newPos;
                    IsDragging = true;
                    if (HasSelection)
                    {
                        AddDragGhost(newPos);
                    }
                }
            }
            if (IsDragging)
            {
                if (HasSelection && dragGhost != null)
                {
                    bool breakDock;
                    double rotation;
                    Point ghostPos = GetPredictedDragPosition(newPos, out rotation, out breakDock, true);
                    dragGhost.SetPositionAndRotation(ghostPos, rotation);
                }
            }
        }

        Point GetPredictedDragPosition(Point input, out double rotation, out bool breakDock, bool dragCorrect=false)
        {
            breakDock = false;
            rotation = 0;
            if (SelectedElement.Docked)
            {
                double sqDist = (input.X - SelectedElement.DockedTo.AbsolutePosition.X) * (input.X - SelectedElement.DockedTo.AbsolutePosition.X) + (input.Y - SelectedElement.AbsolutePosition.Y) * (input.Y - SelectedElement.AbsolutePosition.Y);
                if (sqDist < BREAK_DOCK_SQ_DIST)
                {
                    Point found = SelectedElement.GetDockPosition(new Point(input.X, input.Y), out rotation);
                    var rotator = (SelectedElement.RenderTransform as RotateTransform);
                    if(rotator!=null) rotation -= rotator.Angle;
                    
                    //THIS NEEDS FIXING AFTER WE GET THE LABELS TO KEEP ORIGINAL ROTATION!
                    if (dragCorrect)
                    {
                        found.X -= 18;
                        found.Y -= 18;
                    }
                    return found;
                }
                else
                {
                    breakDock = true;
                    return new Point(input.X-SelectedElement.DockedTo.Position.X, input.Y-SelectedElement.DockedTo.Position.Y);
                }
            }
           
            rotation = 0;
            return new Point(input.X, input.Y);
            
        }

        public void EndDrag()
        {
            dragOffset = default;
            dragFrom = default;
            dragTo = default;
            preDrag = false;
            RemoveDragGhost();
            IsDragging = false;
            baseCanvas.ReleaseMouseCapture();
        }

        private void RemoveDragGhost()
        {
            if(dragGhost!=null) AdornerLayer.GetAdornerLayer(baseCanvas).Remove(dragGhost);
        }

        public bool InBounds(Point point)
        {
            return InBounds(point, baseCanvas);
        }

        private static bool InBounds(Point point, FrameworkElement boundingElement)
        {
            if (point.X < 0 || point.X > boundingElement.ActualWidth || point.Y < 0 || point.Y > boundingElement.ActualHeight) return false;
            return true;
        }
    }
}
