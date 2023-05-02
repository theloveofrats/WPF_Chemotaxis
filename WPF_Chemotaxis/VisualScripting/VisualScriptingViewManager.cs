using System;
using System.Collections.Generic;
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
    internal class VisualScriptingViewManager
    {
        private Canvas baseCanvas;
        private DragAdorner dragGhost;
        public UIElement SelectedElement { get; private set; }

        public VisualScriptingViewManager(Canvas baseCanvas)
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
        }

        public void AddDragGhost()
        {
            if (HasSelection)
            {
                dragGhost = new DragAdorner(baseCanvas, VisualTreeHelper.GetParent(SelectedElement) as UIElement, SelectedElement.RenderSize);
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

        public void UpdateDrag(Point newPos)
        {
            dragGhost.SetPosition(newPos);
        }

        public void RemoveDragGhost()
        {
            if(dragGhost!=null) AdornerLayer.GetAdornerLayer(baseCanvas).Remove(dragGhost);
        }
    }
}
