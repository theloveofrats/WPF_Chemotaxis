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

        public VSUIElement MakeUIFromMenuItem(VSListMenuElement fromMenuElement, Point clickPsn, ILinkable linkedModelElement, Action<object, MouseButtonEventArgs> LeftMouseDownHandler, Action<object, MouseButtonEventArgs> LeftMouseUpHandler, Action<object, MouseButtonEventArgs> RightMouseUpHandler)
        {
            //New base canvas for the element
            VSUIElement newElement = new VSUIElement();
            newElement.Background = new SolidColorBrush(Colors.Transparent);
            newElement.ui_symbol = fromMenuElement.CreateModelElementControl();
            newElement.LinkedModelPart = linkedModelElement;
            newElement.Width = 0;
            newElement.Height = 0;
            Canvas.SetTop(newElement, clickPsn.Y);
            Canvas.SetLeft(newElement, clickPsn.X);

            // Actual UI image for it here
            newElement.Children.Add(newElement.ui_symbol);
            Canvas.SetTop(newElement.ui_symbol, -0.5 * newElement.ui_symbol.Height);
            Canvas.SetLeft(newElement.ui_symbol, -0.5 * newElement.ui_symbol.Width);
            newElement.ui_symbol.MouseLeftButtonDown += (o, e) => LeftMouseDownHandler(o, e);
            newElement.ui_symbol.MouseLeftButtonUp += (o, e) => LeftMouseUpHandler(o, e);
            newElement.ui_symbol.MouseRightButtonUp += (o, e) => RightMouseUpHandler(o, e);

            //Textbox label for the element
            label = new TextBox();
            label.VerticalAlignment = VerticalAlignment.Center;
            label.HorizontalAlignment = fromMenuElement.tagAlignCentre ? HorizontalAlignment.Center : HorizontalAlignment.Left;
            newElement.Children.Add(label);
            Point nametagOffset = fromMenuElement.NametagOffset;
            Canvas.SetTop(label, nametagOffset.Y);
            Canvas.SetLeft(label, nametagOffset.X);

            //Bind the textbox.
            Binding myTargetBinding = new Binding("Name");
            myTargetBinding.Source = newElement.LinkedModelPart;
            myTargetBinding.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(label, TextBox.TextProperty, myTargetBinding);

            main_canvas.Children.Add(newElement);
            return newElement;
        }
    }
}
