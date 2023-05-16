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
using WPF_Chemotaxis.UX;

namespace WPF_Chemotaxis.VisualScripting
{
    internal class VSRelationElement : VSDiagramObject
    {

        VSDiagramObject primary;
        VSDiagramObject secondary;
        public Line RelationLine { get; private set; }
        public ILinkable ModelReation { get; private set; }

        public VSRelationElement(Canvas canvas) : base(canvas)
        {

        }
        public VSRelationElement(VSDiagramObject primary, VSDiagramObject secondary, ILinkable modelRelation, Canvas canvas) : base(canvas)
        {
            this.ModelReation = modelRelation;
            BindControl(primary, secondary);
        }
        public void BindControl(VSDiagramObject primary, VSDiagramObject secondary)
        {
            this.primary = primary; 
            this.secondary = secondary;
            MakeLine();
        }
        public override void Dispose()
        {
            if(this.RelationLine!=null) _mainCanvas.Children.Remove(RelationLine);
            base.Dispose();
        }
        private void MakeLine()
        {
            if (RelationLine == null)
            {
                RelationLine = new();
                RelationLine.Stroke = Brushes.Blue;
                RelationLine.StrokeThickness = 4;
                _mainCanvas.Children.Add(RelationLine);
                Canvas.SetZIndex(RelationLine, -1);
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

            BindingOperations.SetBinding(RelationLine, Line.X1Property, bindingX1);
            BindingOperations.SetBinding(RelationLine, Line.Y1Property, bindingY1);
            BindingOperations.SetBinding(RelationLine, Line.X2Property, bindingX2);
            BindingOperations.SetBinding(RelationLine, Line.Y2Property, bindingY2);
        }
        public bool DuplicateWithNewHandle(VSDiagramObject oldHandle, VSDiagramObject newHandle, out VSRelationElement dupe)
        {
            if (!this.HasHandle(oldHandle))
            {
                dupe = null;
                return false;
            }
            VSDiagramObject newPrimary, newSecondary;
            if (oldHandle == this.primary)
            {
                newPrimary = newHandle;
                newSecondary = secondary;
            }
            else
            {
                newSecondary = newHandle;
                newPrimary = this.primary;
            }
            dupe = new VSRelationElement(newPrimary, newSecondary, this.ModelReation, _mainCanvas);
            return true;
        }
        private void OnHandleMoved(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("Position") || e.PropertyName.Equals("Rotation"))
            {
                this.InvalidateVisual();
            }
        }
        public bool HasHandle(VSDiagramObject handle)
        {
            return (primary == handle || secondary == handle);
        }
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
        }
    }
}
