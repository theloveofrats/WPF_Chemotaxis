using System;
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

namespace WPF_Chemotaxis.VisualScripting
{
    public class VSDiagramObject : Canvas, INotifyPropertyChanged
    {
        public Point Position  { get; private set; }
        public double Rotation { get; private set; }

        public double AbsoluteX
        {
            get
            {
                return TransformToAncestor(_mainCanvas).Transform(Position).X;
            }
        }
        public double AbsoluteY
        {
            get
            {
                return TransformToAncestor(_mainCanvas).Transform(Position).Y;
            }
        }

        protected Canvas _mainCanvas;

        protected static DispatcherTimer _clickTimer;

        public VSDiagramObject(Canvas canvas)
        {
            this._mainCanvas = canvas;
        }

        public virtual void SetPosition(double x, double y)
        {
            Position = new Point(x, y);
            OnPropertyChanged("Position");
        }
        public void SetRotation(double newRotation)
        {
            Rotation = newRotation;
            RotateTransform gt = this.RenderTransform as RotateTransform;
            if (gt == null)
            {
                gt = new RotateTransform();
            }
            gt.Angle = Rotation;
            this.RenderTransform = gt as Transform; 
            OnPropertyChanged("Rotation");
        }
        public void NudgeRotation(double plusRotation)
        {
            Rotation += plusRotation;
            RotateTransform gt = this.RenderTransform as RotateTransform;
            if (gt == null)
            {
                gt = new RotateTransform();
            }
            gt.Angle = Rotation;
            this.RenderTransform = gt as Transform;
            OnPropertyChanged("Rotation");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
            
            //If there's no selection, select sender
            
            System.Diagnostics.Debug.Print(String.Format("Left click at position {0}:{1}", e.GetPosition(this).X, e.GetPosition(this).Y));
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
            System.Diagnostics.Debug.Print(String.Format("Double click at position {0}:{1}", e.GetPosition(this).X, e.GetPosition(this).Y));
        }

        protected virtual void SingleClickMiddle(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Middle) return;

            System.Diagnostics.Debug.Print(String.Format("Middle-click at position {0}:{1}", e.GetPosition(this).X, e.GetPosition(this).Y));
            var selector = VisualScriptingSelectionManager.Current;
            Point clickPsn = e.GetPosition(this);

            //If MMB on selected element, open corresponding parameter window.
            if(selector.HasSelection && selector.SelectedElement == this) 
            { 
                System.Diagnostics.Debug.Print(string.Format("MMB on selected element"));
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
            System.Diagnostics.Debug.Print(String.Format("Right-click at position {0}:{1}", e.GetPosition(this).X, e.GetPosition(this).Y));
        }
    }
}
