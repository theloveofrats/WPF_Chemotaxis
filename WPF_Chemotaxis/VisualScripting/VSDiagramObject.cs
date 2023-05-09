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

        protected void DefaultLeftMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled) return;


            //If there's no selection, select sender
            if (!VisualScriptingSelectionManager.Current.HasSelection)
            {
                selectionManager.SelectElement(sender as UIElement);
            }
            //If there is a selection and it's not the sender, clear it and select the sender
            else if (!sender.Equals(selectionManager.SelectedElement))
            {
                selectionManager.ClearSelection();
                selectionManager.SelectElement(sender as UIElement);
            }

            selectionManager.StartDrag(e.GetPosition(targetCanvas));
            if (selectionManager.HasSelection)
            {
                e.Handled = true;
            }
            else
            {

            }
        }

        protected void DefaultLeftMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled) return;
            var buttonUpOn = sender as UIElement;
            Point clickPsn = e.GetPosition(targetCanvas);

            // DO NOT HANDLE OUT OF BOUNDS. POSSIBLY A MISTAKE?
            if (!selectionManager.InBounds(clickPsn))
            {
                selectionManager.EndDrag();
                return;
            }


            //IF THE ELEMENT FIRING A BUTTON UP IS NOT THE SELECTED ELEMENT, AND WE HAVE DRAGGED FROM THE CANVAS ONTO IT.
            if (buttonUpOn != null && buttonUpOn != selectionManager.SelectedElement && selectionManager.IsDragging)
            {
                System.Diagnostics.Debug.Print(string.Format("Dragged element on other element"));
                //If there was a selection to drag, and not just a random canvas drag, try to link the elements
                if (selectionManager.HasSelection)
                {
                    System.Diagnostics.Debug.Print(string.Format("Selection manger has selection"));
                    ILinkable parent, child;
                    if (TryGetModelElementFromVisual(selectionManager.SelectedElement, out child) && TryGetModelElementFromVisual(buttonUpOn, out parent))
                    {
                        System.Diagnostics.Debug.Print(string.Format("Attempt to attach {0} to {1}", child.Name, parent.Name));
                        if (TryConnectElements(buttonUpOn, selectionManager.SelectedElement))
                        {
                            //System.Diagnostics.Debug.Print(string.Format("CONNECTED HERE!"));
                        }
                        selectionManager.EndDrag();
                        e.Handled = true;
                    }
                    else
                    {

                    }
                }
                // Otherwise, if there was a selected menu item, create the relevant objects.
                else if (selectionManager.SelectedMenuItem != null)
                {
                    CreateNewModelElementFromMenu(selectionManager.SelectedMenuItem, clickPsn);
                    selectionManager.ClearMenuSelection();

                    if (TryConnectElements(buttonUpOn, selectionManager.SelectedElement))
                    {

                    }
                    e.Handled = true;
                }
                selectionManager.EndDrag();
            }

            // If it's a click but not a drag on selected item
            if (buttonUpOn == selectionManager.SelectedElement && !selectionManager.IsDragging)
            {
                System.Diagnostics.Debug.Print(string.Format("Button up without drag on SELECTED"));
                //IF the sender element IS the selection and it was a double click
                if (selectionManager.HasSelection && buttonUpOn == selectionManager.SelectedElement && e.ClickCount > 1)
                {
                    System.Diagnostics.Debug.Print(string.Format("DOUBLE CLICK"));
                    ILinkable clickedOnLink;
                    if (ui_model_multimap.TryGetValue(selectionManager.SelectedElement, out clickedOnLink))
                    {
                        Model.Model.SetNextFocus(clickedOnLink);
                        selectionManager.EndDrag();
                    }
                }
                else
                {
                    selectionManager.EndDrag();
                    e.Handled = true;
                }
            }
            selectionManager.EndDrag();
        }

        protected void DefaultRightMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled) return;
            var buttonUpOn = sender as UIElement;
            Point clickPsn = e.GetPosition(targetCanvas);

            // DO NOT HANDLE OUT OF BOUNDS. POSSIBLY A MISTAKE?
            if (!selectionManager.InBounds(clickPsn))
            {
                selectionManager.EndDrag();
                return;
            }

            //Right click on selected element
            if (selectionManager.HasSelection && buttonUpOn == selectionManager.SelectedElement)
            {
                System.Diagnostics.Debug.Print(string.Format("RIGHT CLICK"));
                ILinkable clickedOnLink;
                if (ui_model_multimap.TryGetValue(selectionManager.SelectedElement, out clickedOnLink))
                {
                    Model.Model.SetNextFocus(clickedOnLink);
                }
                e.Handled = true;
            }
        }
    }
}
