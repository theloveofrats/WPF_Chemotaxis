using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WPF_Chemotaxis.VisualScripting
{
    internal class VSRelationElement : VSDiagramObject
    {

        VSDiagramObject primary;
        VSDiagramObject secondary;
        Line _line;

        public VSRelationElement(Canvas canvas) : base(canvas)
        {

        }
        public VSRelationElement(VSDiagramObject primary, VSDiagramObject secondary, Canvas canvas) : base(canvas)
        {
            BindControl(primary, secondary);
        }
        public void BindControl(VSDiagramObject primary, VSDiagramObject secondary)
        {
            this.primary = primary; 
            this.secondary = secondary;
            MakeLine();
        }

        private void MakeLine()
        {
            if (_line == null)
            {
                _line = new();
                _line.Stroke = Brushes.Blue;
                _line.StrokeThickness = 4;
                _mainCanvas.Children.Add(_line);
                Canvas.SetZIndex(_line, -1);
            }
            Binding bindingX1 = new();
            Binding bindingX2 = new();
            Binding bindingY1 = new();
            Binding bindingY2 = new();

            bindingX1.Source = primary;
            bindingY1.Source = primary;
            bindingX1.Path = new PropertyPath("AbsolutePosition.X");
            bindingY1.Path = new PropertyPath("AbsolutePosition.Y");

            bindingX2.Source = secondary;
            bindingY2.Source = secondary;
            bindingX2.Path = new PropertyPath("AbsolutePosition.X");
            bindingY2.Path = new PropertyPath("AbsolutePosition.Y");

            bindingX1.Mode = BindingMode.TwoWay;
            bindingY1.Mode = BindingMode.TwoWay;
            bindingX2.Mode = BindingMode.TwoWay;
            bindingY2.Mode = BindingMode.TwoWay;

            bindingX1.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            bindingY1.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            bindingX2.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            bindingY2.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

            BindingOperations.SetBinding(_line, Line.X1Property, bindingX1);
            BindingOperations.SetBinding(_line, Line.Y1Property, bindingY1);
            BindingOperations.SetBinding(_line, Line.X2Property, bindingX2);
            BindingOperations.SetBinding(_line, Line.Y2Property, bindingY2);
        }

        private void OnHandleMoved(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("Position") || e.PropertyName.Equals("Rotation"))
            {
                this.InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
        }
    }
}
