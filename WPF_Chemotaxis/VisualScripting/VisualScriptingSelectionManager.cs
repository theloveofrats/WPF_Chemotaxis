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
        private Canvas baseCanvas;
        private Point dragFrom;
        private Point dragTo;
        private DragAdorner dragGhost;
        private bool preDrag = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsDragging { get; private set; }

        public UIElement SelectedElement { get; private set; }

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

        public VisualScriptingSelectionManager(Canvas baseCanvas)
        {
            this.baseCanvas = baseCanvas;
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

        public void SelectElement(UIElement element)
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

        public void RotateSelected(double degrees)
        {
            if (!HasSelection) return;

            var selectedParent = VisualTreeHelper.GetParent(SelectedElement) as Canvas;
            RotateTransform current = selectedParent.RenderTransform as RotateTransform;

            if (current == null)
            {
                selectedParent.RenderTransform = new RotateTransform();
                current = selectedParent.RenderTransform as RotateTransform;
            }
            current.Angle += degrees;
            
            foreach(var child in selectedParent.Children)
            {
                TextBox label = child as TextBox;
                if (label != null)
                {
                    label.RenderTransform = (Transform) current.Inverse;
                }
            }
        }

        private void AddDragGhost(Point clickPsn)
        {
            if (HasSelection)
            {
                UIElement parent = VisualTreeHelper.GetParent(SelectedElement) as UIElement;
                Point offset = baseCanvas.TransformToDescendant(SelectedElement).Transform(clickPsn);
                dragGhost = new DragAdorner(baseCanvas, parent, offset);
                dragGhost.IsHitTestVisible = false;
                AdornerLayer.GetAdornerLayer(baseCanvas).Add(dragGhost);
            }
        }

        public void MoveSelected(Point moveTo)
        {
            MoveElement(VisualTreeHelper.GetParent(SelectedElement) as UIElement, moveTo);
        }

        private void MoveElement(UIElement element, Point dragTo)
        {
            Canvas.SetTop(element, dragTo.Y);
            Canvas.SetLeft(element, dragTo.X);
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
                    IsDragging = true;
                    if (HasSelection)
                    {
                        AddDragGhost(newPos);
                    }
                }
            }
            if (IsDragging)
            {
                if (dragGhost != null)
                {
                    dragGhost.SetPosition(newPos);
                }
            }
        }

        public void EndDrag()
        {
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
