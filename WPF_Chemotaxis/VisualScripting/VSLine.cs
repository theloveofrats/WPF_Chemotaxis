using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
//using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WPF_Chemotaxis.VisualScripting
{
    public enum LineAnchorType {ANCHOR_CENTRE, ANCHOR_FORWARD}
    public enum LineHeadType { NONE, CIRCLE, ARROW, INHIBIT};

    public class VSLine : FrameworkElement
    {
        private bool _disposing;
        public VSDiagramObject ParentVisual { get; private set; }
        public VSDiagramObject ChildVisual  { get; private set; }
        public Color PathColor { get; set; } = Colors.SlateBlue;

        //Dependency property hooks
        public static readonly DependencyProperty X1Property = DependencyProperty.Register(nameof(X1), typeof(double), typeof(VSLine), new PropertyMetadata(0.0, new PropertyChangedCallback(OnLinePropertyChanged)));
        public static readonly DependencyProperty Y1Property = DependencyProperty.Register(nameof(Y1), typeof(double), typeof(VSLine), new PropertyMetadata(0.0, new PropertyChangedCallback(OnLinePropertyChanged)));
        public static readonly DependencyProperty X2Property = DependencyProperty.Register(nameof(X2), typeof(double), typeof(VSLine), new PropertyMetadata(0.0, new PropertyChangedCallback(OnLinePropertyChanged)));
        public static readonly DependencyProperty Y2Property = DependencyProperty.Register(nameof(Y2), typeof(double), typeof(VSLine), new PropertyMetadata(0.0, new PropertyChangedCallback(OnLinePropertyChanged)));

        [System.ComponentModel.TypeConverter(typeof(System.Windows.LengthConverter))]
        public double X1 {
            get {
                return (double)GetValue(X2Property);
            }
            set
            {
                SetValue(X2Property, value); 
            }
        }

        [System.ComponentModel.TypeConverter(typeof(System.Windows.LengthConverter))]
        public double Y1
        {
            get
            {
                return (double)GetValue(X2Property);
            }
            set
            {
                SetValue(X2Property, value);
            }
        }

        [System.ComponentModel.TypeConverter(typeof(System.Windows.LengthConverter))]
        public double X2
        {
            get
            {
                return (double) GetValue(X2Property);
            }
            set
            {
                SetValue(X2Property, value);
            }
        }

        [System.ComponentModel.TypeConverter(typeof(System.Windows.LengthConverter))]
        public double Y2
        {
            get
            {
                return (double)GetValue(X2Property);
            }
            set
            {
                SetValue(X2Property, value);
            }
        }

        [System.ComponentModel.TypeConverter(typeof(System.Windows.LengthConverter))]
        private double IX1 { get; set; }
        [System.ComponentModel.TypeConverter(typeof(System.Windows.LengthConverter))]
        private double IY1 { get; set; }

        [System.ComponentModel.TypeConverter(typeof(System.Windows.LengthConverter))]
        private double IX2 { get; set; }
        [System.ComponentModel.TypeConverter(typeof(System.Windows.LengthConverter))]
        private double IY2 { get; set; }
        [System.ComponentModel.TypeConverter(typeof(System.Windows.LengthConverter))]
        public double X3 { get; set; }
        [System.ComponentModel.TypeConverter(typeof(System.Windows.LengthConverter))]
        public double Y3 { get; set; }
        [System.ComponentModel.TypeConverter(typeof(System.Windows.LengthConverter))]
        public double X4 { get; set; }
        [System.ComponentModel.TypeConverter(typeof(System.Windows.LengthConverter))]
        public double Y4 { get; set; }


        private LineAnchorType ParentAnchor { get; set; }
        private LineAnchorType ChildAnchor { get; set; }
        private LineHeadType ParentHead { get; set; }
        private LineHeadType ChildHead { get; set; }
        private double ParentExclusion { get; set; }
        private double ChildExclusion { get; set; }

        private Canvas _canvas;

        private Path _parentHeadPath = new();
        private Path _childHeadPath = new();

        private Path Path { get; set; }

        private Func<Color> colorFunc;

        public VSLine(VSDiagramObject parent, VSDiagramObject child, Canvas canvas, double parentExclusion, double childExclusion, LineAnchorType parentAnchor, LineAnchorType childAnchor, LineHeadType parentHead, LineHeadType childHead, Func<Color> colorFunc)
        {
            this.DataContext = this;
            this._canvas = canvas;
            this.ParentVisual = parent;
            this.ChildVisual = child;
            this.ParentAnchor = parentAnchor;
            this.ChildAnchor = childAnchor;
            this.ParentHead = parentHead;
            this.ChildHead = childHead;
            this.ParentExclusion = parentExclusion;
            this.ChildExclusion = childExclusion;
            this.Path = new Path();
            this.Path.StrokeThickness = 4.0f;
            this.colorFunc = colorFunc;
            this.MakeBindings();
        }

        private void MakeBindings()
        {
            Binding bindingX1 = new();
            Binding bindingX2 = new();
            Binding bindingY1 = new();
            Binding bindingY2 = new();

            bindingX1.Source = ParentVisual;
            bindingY1.Source = ParentVisual;
            bindingX1.Path = new PropertyPath("AbsolutePosition.X");
            bindingY1.Path = new PropertyPath("AbsolutePosition.Y");

  
            bindingX2.Source = ChildVisual;
            bindingY2.Source = ChildVisual;
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

            BindingOperations.SetBinding(this, VSLine.X1Property, bindingX1);
            BindingOperations.SetBinding(this, VSLine.Y1Property, bindingY1);
            BindingOperations.SetBinding(this, VSLine.X2Property, bindingX2);
            BindingOperations.SetBinding(this, VSLine.Y2Property, bindingY2);
        }

        private void ClearBindings()
        {
            BindingOperations.ClearBinding(this, VSLine.X1Property);
            BindingOperations.ClearBinding(this, VSLine.Y1Property);
            BindingOperations.ClearBinding(this, VSLine.X2Property);
            BindingOperations.ClearBinding(this, VSLine.Y2Property);
        }

        /// <summary>
        /// Redraws the bezier path after the control points are changed.
        /// </summary>
        private void MakePath()
        {
            if(_disposing)
            {
                return;
            }
            recalculated = true;
            RecalculateCPs();
            this.PathColor = colorFunc();

            PathGeometry path = new PathGeometry();
            PathFigure pathFigure = new PathFigure();
            pathFigure.StartPoint = new Point(IX1, IY1);
            //var curve = new QuadraticBezierSegment();
            var curve = new BezierSegment();
            curve.Point1 = new Point(X3, Y3);
            curve.Point2 = new Point(X4, Y4);
            curve.Point3 = new Point(IX2, IY2);
            pathFigure.Segments.Add(curve);

            path.Figures.Add(pathFigure);
            this.Path.Stroke = new SolidColorBrush(this.PathColor);
            this.Path.Data = path;
            if (!_canvas.Children.Contains(this.Path))
            {
                _canvas.Children.Add(this.Path);
            }

            if(IX1==X3 && IY1 == Y3)
            {
                Arrowhead(curve.Point2, new Point(IX1, IY1), ParentHead, _parentHeadPath);
            }
            else
            {
                Arrowhead(curve.Point1, new Point(IX1, IY1), ParentHead, _parentHeadPath);
            }

            if (IX2 == X4 && IY2 == Y4)
            {
                Arrowhead(curve.Point1, curve.Point3, ChildHead, _childHeadPath);
            }
            else
            {
                Arrowhead(curve.Point2, curve.Point3, ChildHead, _childHeadPath);
            }
        }

        private void Arrowhead(Point origin, Point destination, LineHeadType type, Path targetPath)
        {
            PathFigure head = new PathFigure();
            switch (type)
            {
                case LineHeadType.NONE:
                    break;
                case LineHeadType.CIRCLE:
                    EllipseGeometry geo = new EllipseGeometry();
                    geo.Center = destination;
                    geo.RadiusX = 2.5;
                    geo.RadiusY = 2.5;
                    targetPath.Data = geo;
                    targetPath.StrokeThickness = 4;
                    targetPath.Stroke = new SolidColorBrush(this.PathColor);
                    if (!_canvas.Children.Contains(targetPath))
                    {
                        _canvas.Children.Add(targetPath);
                    }

                    break;
                case LineHeadType.ARROW:

                    var direction = (destination - origin);
                    direction.Normalize();
                    direction *= 9.0;
                    LineGeometry lineGeo = new LineGeometry();
                    lineGeo.StartPoint = destination - direction;
                    lineGeo.EndPoint = destination+0.01*direction;

                    targetPath.Data = lineGeo;
                    targetPath.StrokeThickness = 18;
                    targetPath.Stroke = new SolidColorBrush(this.PathColor);
                    targetPath.StrokeEndLineCap = PenLineCap.Triangle;
                    if (!_canvas.Children.Contains(targetPath))
                    {
                        _canvas.Children.Add(targetPath);
                    }
                    break;

                case LineHeadType.INHIBIT:

                    direction = (destination - origin);
                    direction = new Vector(direction.Y, - direction.X);
                    direction.Normalize();
                    direction *= 9.0;
                    LineGeometry lineGeo2 = new LineGeometry();
                    lineGeo2.StartPoint = destination - direction;
                    lineGeo2.EndPoint = destination + direction;

                    targetPath.Data = lineGeo2;
                    targetPath.StrokeThickness = 4;
                    targetPath.Stroke = new SolidColorBrush(this.PathColor);
                    if (!_canvas.Children.Contains(targetPath))
                    {
                        _canvas.Children.Add(targetPath);
                    }
                    break;
            }
        }

        private void RecalculateCPs()
        {

            Point parPt, childPt, ctrlPtA, ctrlPtB;

            var crowFlies   = ParentVisual.AbsolutePosition - ChildVisual.AbsolutePosition;
            double crowDist = crowFlies.Length;

            if (ParentAnchor == LineAnchorType.ANCHOR_FORWARD)
            {
                parPt   = ParentVisual.TranslatePoint(new Point(0, -ParentExclusion), _canvas);
                ctrlPtA = ParentVisual.TranslatePoint(new Point(0, -0.65*crowDist), _canvas);
            }
            else
            {
                parPt   = new Point((ParentVisual.AbsolutePosition.X + crowFlies.X * ParentExclusion / crowDist), 
                                  (ParentVisual.AbsolutePosition.Y + crowFlies.Y * ParentExclusion / crowDist));
                ctrlPtA = parPt;
            }

            var incomingVector = (ctrlPtA-ChildVisual.AbsolutePosition);
            incomingVector.Normalize();

            if (ChildAnchor == LineAnchorType.ANCHOR_FORWARD)
            {
                childPt = ChildVisual.TranslatePoint(new Point(0, -ChildExclusion), _canvas);
                ctrlPtB = ChildVisual.TranslatePoint(new Point(0, -0.65 * crowDist), _canvas);
            }
            else
            {
                childPt = new Point((ChildVisual.AbsolutePosition.X + incomingVector.X * ChildExclusion),
                                    (ChildVisual.AbsolutePosition.Y + incomingVector.Y * ChildExclusion));
                ctrlPtB = childPt;
            }

            IX1 = parPt.X;
            IY1 = parPt.Y;
            IX2 = childPt.X;
            IY2 = childPt.Y;
            X3 = ctrlPtA.X;
            Y3 = ctrlPtA.Y;
            X4 = ctrlPtB.X;
            Y4 = ctrlPtB.Y;
        }

        protected bool recalculated;
        private static async void OnLinePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = ((VSLine)d);
            target.recalculated = false;
            await Task.Delay(5);
            if(!target.recalculated) ((VSLine)d).MakePath();
        }

        public void Clean()
        {
            _disposing = true;
            if(_canvas.Children.Contains(this.Path))
            {
                _canvas.Children.Remove(this.Path);
            }
            if (_canvas.Children.Contains(this._parentHeadPath))
            {
                _canvas.Children.Remove(this._parentHeadPath);
            }
            if (_canvas.Children.Contains(this._childHeadPath))
            {
                _canvas.Children.Remove(this._childHeadPath);
            }
            ClearBindings();
        }
    }
}
