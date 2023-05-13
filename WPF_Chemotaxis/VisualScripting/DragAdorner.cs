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
        private double baseAngle = 0;
        private double targetAngle = 0;
        public DragAdorner(UIElement adorned, UIElement brushImg, Point internalOffset) : base(adorned)
        {
            vBrush = new VisualBrush(brushImg);

            RotateTransform incomingRotation = brushImg.RenderTransform as RotateTransform;
            if (incomingRotation != null)
            {
                baseAngle = incomingRotation.Angle;
            }

            vBrush.Opacity = 0.6;
            Transform brushTrans = brushImg.RenderTransform;
            Rect trans_size = brushTrans.TransformBounds(VisualTreeHelper.GetDescendantBounds(brushImg));
            drawSize = new Size(trans_size.Width+40, trans_size.Height+40);

            Rect totalBounds = VisualTreeHelper.GetDescendantBounds(brushImg);
            Rect innerBounds = VisualTreeHelper.GetContentBounds(brushImg);

            offset = new Point(internalOffset.X + innerBounds.X - totalBounds.X, internalOffset.Y + innerBounds.Y - totalBounds.Y);
        }

        public void SetPosition(Point position)
        {
            this.location = new Point(position.X-(offset.X+20), position.Y-(offset.Y+20));
            this.InvalidateVisual();
        }
        public void SetPositionAndRotation(Point position, double newAngle)
        {
            this.targetAngle = newAngle - baseAngle;
            this.location = new Point(position.X - (offset.X + 20), position.Y - (offset.Y + 20));
            this.InvalidateVisual();
        }

        /*public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            if(targetAngle==0) return base.GetDesiredTransform(transform);
                       
            GeneralTransformGroup target = new();
            target.Children.Add(new RotateTransform(targetAngle)); 
            target.Children.Add(base.GetDesiredTransform(transform));
            return target;
        }*/

        protected override void OnRender(DrawingContext dc)
        {
            dc.DrawRectangle(vBrush, null, new Rect(location, drawSize));
        }
    }
}
