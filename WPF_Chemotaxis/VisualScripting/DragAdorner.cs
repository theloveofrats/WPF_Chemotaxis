using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace WPF_Chemotaxis.VisualScripting
{
    internal class DragAdorner : Adorner
    {
        private Point location;
        private VisualBrush vBrush;
        private Size drawSize;
        private Point offset;
        public DragAdorner(UIElement adorned, UIElement brushImg, Size targetRenderSize) : base(adorned)
        {
            vBrush = new VisualBrush(brushImg);

            vBrush.Opacity = 0.6;

            Rect trans_size = brushImg.RenderTransform.TransformBounds(new Rect(0, 0, targetRenderSize.Width, targetRenderSize.Height));

            drawSize = new Size(trans_size.Width+40, trans_size.Height+40);
            this.offset = new Point(0.5*drawSize.Width, 0.5*drawSize.Height);
        }

        public void SetPosition(Point position)
        {
            this.location = new Point(position.X-offset.X, position.Y-offset.Y);
            this.InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            dc.DrawRectangle(vBrush, null, new Rect(location, drawSize));
        }
    }
}
