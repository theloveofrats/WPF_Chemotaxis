using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;

namespace WPF_Chemotaxis.VisualScripting
{
    public class VSDiagramObject : Canvas, INotifyPropertyChanged
    {
        public Point Position  { get; private set; }
        public double Rotation { get; private set; }

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
            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
