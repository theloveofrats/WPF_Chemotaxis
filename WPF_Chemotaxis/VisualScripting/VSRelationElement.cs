using System;
using System.Reflection;
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

        private List<Line> _lines;
        private Dictionary<Line, VSDiagramObject> _lineTargets;
        public IReadOnlyCollection<Line> RelationLines
        {
            get
            {
                return _lines;
            }
        }
        //public Line RelationLine { get; private set; }
        public ILinkable ModelReation { get; private set; }

        public VSRelationElement(Canvas canvas) : base(canvas)
        {

        }
        public VSRelationElement(VSDiagramObject primary, ILinkable modelRelation, Canvas canvas) : base(canvas)
        {
            this.ModelReation = modelRelation;
            this.primary = primary;
            //if(this.Parent == null)
            //{
            //    _mainCanvas.Children.Add(this);
            //}
            MakeLines();

        }
        public override void Dispose()
        {
            CleanLines();
            base.Dispose();
        }
        private void MakeLines()
        {
            System.Diagnostics.Debug.Print(String.Format("Reached VSRelationElement.MakeLines()"));
            ILinkable checkLinkDebug;

            if (VSModelManager.Current.TryGetModelElementFromVisual(primary, out checkLinkDebug)) {
               
            }
            else
            {
                System.Diagnostics.Debug.Print("No model element for the parent visual!");
            }
            var lineFields = this.ModelReation.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where((field) => field.GetCustomAttribute<VisualLineAttribute>() != null);
            if (lineFields?.Count() > 0)
            {
                foreach (var field in lineFields)
                {
                    VisualLineAttribute vla = field.GetCustomAttribute<VisualLineAttribute>();
                    List<VSDiagramObject> lineTargetUIs = new List<VSDiagramObject>();
                    var lineLink = field.GetValue(this.ModelReation) as ILinkable;
                    if (lineLink != null)
                    {
                        if (VSModelManager.Current.TryGetUIListFromLink(lineLink, out lineTargetUIs))
                        {
                            System.Diagnostics.Debug.Print(string.Format("Model part {0} has {1} UIs to link lines to", lineLink.Name, lineTargetUIs.Count()));
                            foreach (var lineTarget in lineTargetUIs)
                            {
                                MakeLine(lineTarget, vla);
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.Print(string.Format("lineLink was null- no iLinkable in the field {0} for {1}", field.Name, ModelReation.Name));
                    }
                }
                System.Diagnostics.Debug.Print("Finished making lines");
            }
        }

        private void MakeLine(VSDiagramObject lineTarget, VisualLineAttribute annotation) {

            if (_lines == null) _lines = new();
            if (_lineTargets == null) _lineTargets = new();

            System.Diagnostics.Debug.Print(string.Format("Making line from primary handle to target"));

            Line newLine = new();

            newLine.Stroke = Brushes.Blue;
            newLine.StrokeThickness = 4;
            newLine.StrokeStartLineCap = annotation.lineType == LineType.ARROW_FROM ? PenLineCap.Triangle : PenLineCap.Square;
            newLine.StrokeEndLineCap   = annotation.lineType == LineType.ARROW_TO ?   PenLineCap.Triangle : PenLineCap.Square;

            _mainCanvas.Children.Add(newLine);
            Canvas.SetZIndex(newLine, -1);

            _lines.Add(newLine);
            _lineTargets.Add(newLine, lineTarget);

            Binding bindingX1 = new();
            Binding bindingX2 = new();
            Binding bindingY1 = new();
            Binding bindingY2 = new();

            bindingX1.Source = primary;
            bindingY1.Source = primary;
            bindingX1.Path = new PropertyPath("AbsolutePosition.X");
            bindingY1.Path = new PropertyPath("AbsolutePosition.Y");

            System.Diagnostics.Debug.Print(string.Format("Absolute position of primary handle ({0:0.0}:{1:0.0}).", primary.AbsolutePosition.X, primary.AbsolutePosition.Y));

            bindingX2.Source = lineTarget;
            bindingY2.Source = lineTarget;
            bindingX2.Path = new PropertyPath("AbsolutePosition.X");
            bindingY2.Path = new PropertyPath("AbsolutePosition.Y");

            System.Diagnostics.Debug.Print(string.Format("Absolute position of secondary handle ({0:0.0}:{1:0.0}).", lineTarget.AbsolutePosition.X, lineTarget.AbsolutePosition.Y));

            System.Diagnostics.Debug.Print(string.Format("Recorded positions at StackTrace:: {0}", Environment.StackTrace));

            bindingX1.Mode = BindingMode.TwoWay;
            bindingY1.Mode = BindingMode.TwoWay;
            bindingX2.Mode = BindingMode.TwoWay;
            bindingY2.Mode = BindingMode.TwoWay;

            bindingX1.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            bindingY1.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            bindingX2.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            bindingY2.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

            BindingOperations.SetBinding(newLine, Line.X1Property, bindingX1);
            BindingOperations.SetBinding(newLine, Line.Y1Property, bindingY1);
            BindingOperations.SetBinding(newLine, Line.X2Property, bindingX2);
            BindingOperations.SetBinding(newLine, Line.Y2Property, bindingY2);
        }

        public bool DuplicateWithNewHandle(VSDiagramObject oldHandle, VSDiagramObject newHandle, out VSRelationElement dupe)
        {
            if (!this.HasHandle(oldHandle))
            {
                dupe = null;
                return false;
            }
            if (oldHandle == this.primary) // i.e. if the primary was duplicated, we create a new relation object
            {
                dupe = new VSRelationElement(newHandle, this.ModelReation, _mainCanvas);
                return true;
            }
            else  // Otherwise, we add it to the lines needed here from the original primary...
            {
                CleanLines();
                MakeLines();
                dupe = null;
                return false;
            }            
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
            return (primary == handle || _lineTargets.Values.Contains(handle));
        }
        private void CleanLines()
        {
            foreach (var line in _lines)
            {
                _mainCanvas.Children.Remove(line);
            }
            _lines.Clear();
            _lineTargets.Clear();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
        }
    }
}
