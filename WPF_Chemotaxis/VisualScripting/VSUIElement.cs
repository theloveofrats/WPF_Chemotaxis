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
        private Canvas main_canvas;

        private VSUIElement() { }

        public VSUIElement(VSListMenuElement fromMenuElement, Point clickPsn, ILinkable linkedModelElement, Canvas main_canvas, Action<object, MouseButtonEventArgs> LeftMouseDownHandler, Action<object, MouseButtonEventArgs> LeftMouseUpHandler, Action<object, MouseButtonEventArgs> RightMouseUpHandler)
        {
            //New base canvas for the element

            this.Background = new SolidColorBrush(Colors.Transparent);
            this.ui_symbol = fromMenuElement.CreateModelElementControl();
            this.LinkedModelPart = linkedModelElement;
            this.Width = 0;
            this.Height = 0;
            Canvas.SetTop(this, clickPsn.Y);
            Canvas.SetLeft(this, clickPsn.X);

            // Actual UI image for it here
            this.Children.Add(this.ui_symbol);
            Canvas.SetTop(this.ui_symbol, -0.5 * this.ui_symbol.Height);
            Canvas.SetLeft(this.ui_symbol, -0.5 * this.ui_symbol.Width);
            this.ui_symbol.MouseLeftButtonDown += (o, e) => LeftMouseDownHandler(o, e);
            this.ui_symbol.MouseLeftButtonUp += (o, e) => LeftMouseUpHandler(o, e);
            this.ui_symbol.MouseRightButtonUp += (o, e) => RightMouseUpHandler(o, e);

            //Textbox label for the element
            label = new TextBox();
            label.VerticalAlignment = VerticalAlignment.Center;
            label.HorizontalAlignment = fromMenuElement.tagAlignCentre ? HorizontalAlignment.Center : HorizontalAlignment.Left;
            this.Children.Add(label);
            Point nametagOffset = fromMenuElement.NametagOffset;
            Canvas.SetTop(label, nametagOffset.Y);
            Canvas.SetLeft(label, nametagOffset.X);

            //Bind the textbox.
            Binding myTargetBinding = new Binding("Name");
            myTargetBinding.Source = this.LinkedModelPart;
            myTargetBinding.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(label, TextBox.TextProperty, myTargetBinding);

            main_canvas.Children.Add(this);
        }
    }
}
