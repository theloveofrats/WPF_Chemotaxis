using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WPF_Chemotaxis.Model;
using WPF_Chemotaxis.UX;

namespace WPF_Chemotaxis.VisualScripting
{
    /// <summary>
    /// Represents any single instance of a UI image that has a particular model element associated with it. 
    /// Two instances of this class can target the same element, but will have it represented in different places:
    /// for example, two cells may express the same receptor- 
    /// </summary>
    public class VSUIElement : VSDiagramObject
    {
        public ILinkable LinkedModelPart { get; private set; }
        private Image ui_symbol;
        private TextBox label;

        public VSUIElement(VSListMenuElement fromMenuElement, Point clickPsn, ILinkable linkedModelElement, Canvas main_canvas) : base(main_canvas)
        {
            //New base canvas for the element


            this.Background = new SolidColorBrush(Colors.Transparent);
            this.ui_symbol = fromMenuElement.CreateModelElementControl();
            this.LinkedModelPart = linkedModelElement;
            this.Width = 0;
            this.Height = 0;

            // Actual UI image for it here
            this.Children.Add(this.ui_symbol);
            Canvas.SetTop(this.ui_symbol, -0.5 * this.ui_symbol.Height);
            Canvas.SetLeft(this.ui_symbol, -0.5 * this.ui_symbol.Width);
            //this.ui_symbol.MouseLeftButtonUp += (o, e) => this.DefaultLeftMouseDown(o, e);
            this.ui_symbol.MouseLeftButtonDown += (o, e) => this.HandleLeftMouseDownEvent(o, e);
            this.ui_symbol.MouseLeftButtonUp   += (o, e) => this.SingleClickLeftUp(o, e);
            this.ui_symbol.MouseRightButtonUp  += (o, e) => this.SingleClickRight(o, e);
            this.ui_symbol.MouseUp += (o, e) => this.SingleClickMiddle(o,e);
            //Textbox label for the element
            label = new TextBox();
            label.VerticalAlignment = VerticalAlignment.Center;
            label.HorizontalAlignment = fromMenuElement.tagAlignCentre ? HorizontalAlignment.Center : HorizontalAlignment.Left;
            label.KeyUp += (s, e) =>
            {
                if (e.Key == Key.Return || e.Key == Key.Enter)
                {
                    Keyboard.Focus(_mainCanvas);
                }
            };

            this.Children.Add(label);
            Point nametagOffset = fromMenuElement.NametagOffset;
            Canvas.SetTop(label, nametagOffset.Y);
            Canvas.SetLeft(label, nametagOffset.X);
            //Bind the textbox.
            Binding myTargetBinding = new Binding("Name");
            myTargetBinding.Source = this.LinkedModelPart;
            myTargetBinding.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(label, TextBox.TextProperty, myTargetBinding);

            SetPosition(clickPsn.X, clickPsn.Y);
            System.Diagnostics.Debug.Print(String.Format("Current position is {0:0.0}:{1:0.0}.", this.AbsolutePosition.X, this.AbsolutePosition.Y));
        }

        protected override void SingleClickLeftUp(object sender, MouseButtonEventArgs e)
        {
            var selector = VisualScriptingSelectionManager.Current;
            // Base always handles if this is a mouse up on a selected item. We're only interested in drags here.
            base.SingleClickLeftUp(sender, e);
            if (e.Handled)
            {
                System.Diagnostics.Debug.Print(String.Format("Already handled!"));
                selector.EndDrag();
                return;
            }

            
            if (selector.HasSelection && selector.SelectedElement!=this && selector.IsDragging && selector.InBounds(e.GetPosition(_mainCanvas)))
            {
                System.Diagnostics.Debug.Print(String.Format("Meets criteria for join"));
                //If we're dragging on a proper element control and not another kind of diagram object
                if (selector.SelectedElement.GetType().IsAssignableTo(typeof(VSUIElement)))
                {
                    if (VSModelManager.Current.TryConnectElements(parentVisual: this, childVisual: selector.SelectedElement as VSUIElement))
                    {
                        System.Diagnostics.Debug.Print("Connected!");
                    }
                    else
                    {
                        System.Diagnostics.Debug.Print("Could not connect!");
                    }
                }
                //For other diagram objects...
                else
                {

                }
            }
            Keyboard.Focus(_mainCanvas);
            selector.EndDrag();
        }
    }
}
