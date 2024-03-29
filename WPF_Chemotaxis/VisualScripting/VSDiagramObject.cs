﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;
using WPF_Chemotaxis.UX;
using System.Reflection.Metadata;

namespace WPF_Chemotaxis.VisualScripting
{
    public class VSDiagramObject : Canvas, INotifyPropertyChanged
    {
        public Point Position
        {
            get
            {
                return new Point(Canvas.GetLeft(this)-0.5*this.ActualWidth, Canvas.GetTop(this)-0.5*this.ActualHeight);
            }
            private set
            {
                Point target = value;
                Canvas.SetTop(this, target.Y+0.5*this.ActualWidth);
                Canvas.SetLeft(this, target.X+0.5*this.ActualHeight);
            }
        }
        public Point AbsolutePosition
        {
            get
            {
                if (this.Parent == _mainCanvas)
                {
                    return Position;
                }
                else
                {
                    return (this.Parent as Canvas).TransformToAncestor(_mainCanvas).Transform(Position);
                }
            }
        }
        public bool Disposing { get; private set; }
        public double Rotation { get; private set; }

        public bool Docked
        {
            get
            {
                return DockedTo != null; 
            }
        }
        public VSDiagramObject DockedTo { get; private set; }
        private double dockDistance;

        protected Canvas _mainCanvas;

        protected static DispatcherTimer _clickTimer;

        public VSDiagramObject(Canvas canvas)
        {
            this._mainCanvas = canvas;
            _mainCanvas.Children.Add(this);
        }
        public virtual VSDiagramObject Duplicate()
        {
            VSDiagramObject newObj = new VSDiagramObject(_mainCanvas);
            return newObj;
        }

        public virtual void SetPosition(double x, double y)
        {
            Point newpt = new Point(x, y);
            SetPosition(newpt);            
        }
        public virtual void SetPosition(Point pt)
        {
            Position = pt;
        }
        public virtual void SetAbsolutePosition(Point pt)
        {
            if (this.Parent == _mainCanvas)
            {
                SetPosition(pt);
            }
            else
            {
                SetPosition(_mainCanvas.TransformToDescendant(this.Parent as Canvas).Transform(pt));
            }
        }

        public void SetRotation(double newRotation)
        {
            Rotation = newRotation;
            RotateTransform gt = this.RenderTransform as RotateTransform;
            if (gt == null)
            {
                gt = new RotateTransform(newRotation);
                this.RenderTransform = gt as Transform;
            }
            else
            {
                gt.Angle = Rotation;
            }
            UnrotateLabels();
        }

        private double GetRelativeRotation(Canvas relativeTo)
        {
            double angle = 0;
            if (relativeTo.IsAncestorOf(this))
            {
                Canvas current = this;
                do
                {
                    var rt = (current.RenderTransform as RotateTransform);
                    if(rt!=null) angle += rt.Angle;
                    
                    current = current.Parent as Canvas;
                } 
                while (current!=null && current!=relativeTo);
            }
            return angle;
        }

        private void UnrotateLabels()
        {
            var iter = Children.OfType<TextBox>();
            if (iter != null)
            {
                foreach (var label in iter)
                {
                   
                    double angle = GetRelativeRotation(_mainCanvas);
                    if (angle != 0) { 
                        var render = (label.RenderTransform as RotateTransform);
                        if (render == null)
                        {
                            render = new RotateTransform();
                        }
                        render.Angle = -angle;
                        label.RenderTransform = render;
                    }
                }
            }
        }

        public void NudgeRotation(double plusRotation)
        {
            SetRotation(Rotation+plusRotation);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected async Task OnPropertyChanged(string name, int msDelay)
        {
            await Task.Delay(msDelay);
            var castElement = this as VSUIElement;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected async void ParentPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("Position") || e.PropertyName.Equals("AbsolutePosition"))
            {
                await OnPropertyChanged("AbsolutePosition", 20);
            }
            else if (e.PropertyName.Equals("Rotation"))
            {
                SetAbsolutePosition(AbsolutePosition);
                SetRotation(Rotation);
              
            }
        }

        public virtual void Dispose()
        {
            Disposing = true;
            (Parent as Canvas).Children.Remove(this);
        }

        public async Task ResetLabels()
        {
            await Task.Delay(10);
            foreach(var child in this.Children)
            {
                var textbox = child as TextBox;
                if(textbox!=null)
                {
                    textbox.TransformToAncestor(_mainCanvas);
                    RotateTransform gt = textbox.TransformToAncestor(_mainCanvas) as RotateTransform;
                    RotateTransform rt = textbox.RenderTransform as RotateTransform;

                    if(gt!=null && rt != null)
                    {
                        rt.Angle = gt.Angle;
                    }
                }
            }
        }

        //Manages docking an object and hooking up listeners.
        public void DockToVSObject(VSDiagramObject  dockParent, double dockDistance)
        {
            Point cachedAbsolutePosition = AbsolutePosition;
            if (Parent == dockParent) {

            }
            else { 
                this.DockedTo = dockParent;
                this.dockDistance = dockDistance;
                dockParent.PropertyChanged += this.ParentPropertyChanged;
                SetToDockedPosition();
                Canvas oldParent = Parent as Canvas;
                oldParent?.Children.Remove(this);
                dockParent.Children.Add(this);
                ResetLabels();
            }
            _mainCanvas.InvalidateVisual();
        }
        //FromDropPosition will come from canvas, not from move.
        public Point GetDockPosition(Point fromDropPosition, out double rotationAngle)
        {
            if (!Docked)
            {
                //Not docked, so return the drop position- which is already relative to canvas.
                rotationAngle = (RenderTransform as RotateTransform).Angle;
                return fromDropPosition;
            }
            Vector currentRelative = Point.Subtract(fromDropPosition, DockedTo.AbsolutePosition);
            double targetAngleRadians = Math.Atan2(currentRelative.Y, currentRelative.X);
            rotationAngle = 90d + 180d * targetAngleRadians / Math.PI;
            
            //POsition relative to DockedTo.
            Point targetPoint = new Point(dockDistance * Math.Cos(targetAngleRadians), dockDistance * Math.Sin(targetAngleRadians));

            return targetPoint;
        }

        public void SetToDockedPosition()
        {
            double angle;
            Point newPos = GetDockPosition(AbsolutePosition, out angle);
            //_mainCanvas.TransformToDescendant(dockedTo).Transform(
            RotateTransform rt = DockedTo.RenderTransform as RotateTransform;
            if (rt != null)
            {
                newPos = rt.Inverse.Transform(newPos);
                newPos = rt.Inverse.Transform(newPos);
                angle -= 2.0*rt.Angle;
            }

            this.SetPosition(newPos);
            this.SetRotation(angle);
            
            /*
            foreach (var iter in Children)
            {
                TextBox label = iter as TextBox;
                if (label != null)
                {
                    label.RenderTransform = label.TransformToAncestor(_mainCanvas) as Transform;
                }
            }
            */
        }

        public void Undock()
        {
            if (DockedTo == null) return;
            DockedTo.PropertyChanged -= this.ParentPropertyChanged;
            DockedTo.Children.Remove(this);
            this.RenderTransform = new RotateTransform(0);

            VSModelManager.Current.TryDisconnectElements(DockedTo, this);
            
            _mainCanvas.Children.Add(this);
            DockedTo = null;
            dockDistance = -1;
        }

        public virtual void Clean()
        {
            if (DockedTo != null)
            {
                DockedTo.PropertyChanged -= this.ParentPropertyChanged;
            }
        }

        protected bool HasDaughter(ILinkable link)
        {
            foreach(var el in this.Children.OfType<VSUIElement>())
            {
                if (el.LinkedModelPart == link)
                {
                    return true;
                }
            }
            return false;
        }

        protected static object cachedClickSender;
        protected static MouseButtonEventArgs cachedSingleClickArgs;
        protected void HandleLeftMouseDownEvent(object sender, MouseButtonEventArgs e)
        {
            VisualScriptingSelectionManager selector = VisualScriptingSelectionManager.Current; 
            if (!selector.HasSelection)
            {
                selector.SelectElement(this);
            }
            //If there is a selection and it's not the sender, clear it and select the sender
            else if (!sender.Equals(selector.SelectedElement))
            {
                selector.ClearSelection();
                selector.SelectElement(this);
            }

            selector.StartDrag(e.GetPosition(this.Parent as Canvas));
            if (selector.HasSelection)
            {
                e.Handled = true;
            }
            if (_clickTimer == null)
            {
                _clickTimer = new();
                _clickTimer.Interval = new TimeSpan(0, 0, 0, 0, 250);
            }
                
            if (e.ClickCount == 1)
            {
                _clickTimer.Tick += PostTick;
                cachedClickSender = sender;
                cachedSingleClickArgs = e;
                _clickTimer.Start();
            }
            else if (e.ClickCount > 1)
            {
                cachedClickSender = null;
                cachedSingleClickArgs = null;
                _clickTimer.Stop();
                DoubleClickLeft(sender, e);
            }
        }

        protected void PostTick(object sender, EventArgs e)
        {
            _clickTimer.Stop();
            SingleClickLeft(cachedClickSender, cachedSingleClickArgs);
            cachedClickSender = null;
            cachedSingleClickArgs = null;
            _clickTimer.Tick -= PostTick;
        }

        //This is actually only routed to on a mouse down, so generally shouldn't do anything because mouse down already controls selection.
        //However, for mouse up after a drag, this isn't true. This is handled elsewhere.
        protected virtual void SingleClickLeft(object sender, MouseButtonEventArgs e)
        {
            if (e == null) return;
        }

        //Needs to only handle drags! So we mark the click as handled if it is a mouse up on the selected element.
        protected virtual void SingleClickLeftUp(object sender, MouseButtonEventArgs e)
        {
            var selector = VisualScriptingSelectionManager.Current;

            if (selector.HasSelection && selector.SelectedElement==this)
            {
                selector.EndDrag();
                e.Handled = true;
            }
        }

        protected virtual void DoubleClickLeft(object sender, MouseButtonEventArgs e)
        {
            //System.Diagnostics.Debug.Print(String.Format("Double click at position {0}:{1}", e.GetPosition(this).X, e.GetPosition(this).Y));
        }

        protected virtual void SingleClickMiddle(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Middle) return;

            var selector = VisualScriptingSelectionManager.Current;
            Point clickPsn = e.GetPosition(this);

            //If MMB on selected element, open corresponding parameter window.
            if(selector.HasSelection && selector.SelectedElement == this) 
            { 
                var modelCast = (VSUIElement)this;
                if (modelCast != null)
                {
                    Model.Model.SetNextFocus(modelCast.LinkedModelPart);
                }

                e.Handled = true;
            }
        }

        protected virtual void SingleClickRight(object sender, MouseButtonEventArgs e)
        {
            //System.Diagnostics.Debug.Print(String.Format("Right-click at position {0}:{1}", e.GetPosition(this).X, e.GetPosition(this).Y));
        }

        protected override void OnRender(DrawingContext dc)
        {
            //OnPropertyChanged("Position", 2);
            //OnPropertyChanged("AbsolutePosition", 2);
            //OnPropertyChanged("Rotation", 2);
            base.OnRender(dc);
        }
    }
}
