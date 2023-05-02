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
using WPF_Chemotaxis.UX;

namespace WPF_Chemotaxis.VisualScripting
{
    internal class VisualModelElementFactory
    {
        private Canvas targetCanvas;
        public VisualModelElementFactory(Canvas targetCanvas)
        {
            this.targetCanvas = targetCanvas;
        }
        public UIElement CreateModelElementImage(VSViewModelElement fromMenuElement, Point clickPsn,  VSModelManager vsModelManager, Action<object, MouseButtonEventArgs> LeftMouseDownHandler = null, Action<object, MouseButtonEventArgs> LeftMouseUpHandler = null)
        {
            Point nametagOffset = fromMenuElement.NametagOffset;
            Canvas imageParent = new Canvas();
            imageParent.Background = new SolidColorBrush(Colors.Transparent);

            Image img = fromMenuElement.CreateModelElementControl();
            Canvas.SetTop(imageParent, clickPsn.Y);
            Canvas.SetLeft(imageParent, clickPsn.X);
            Canvas.SetTop(img, - 0.5 * img.Height);
            Canvas.SetLeft(img, - 0.5 * img.Width);
            targetCanvas.Children.Add(imageParent);
            imageParent.Children.Add(img);
            img.MouseLeftButtonDown += (o,e) => LeftMouseDownHandler(o,e);
            img.MouseLeftButtonUp   += (o,e) => LeftMouseUpHandler(o,e);
            vsModelManager.AddNewModelPart(img, fromMenuElement);
            TextBox nameBox = new TextBox();

            ILinkable elem;
            if(vsModelManager.TryGetModelElementFromVisual(img, out elem))
            {
                Binding myTargetBinding = new Binding("Name");
                myTargetBinding.Source = elem;
                myTargetBinding.Mode = BindingMode.TwoWay;
                BindingOperations.SetBinding(nameBox, TextBox.TextProperty, myTargetBinding);
                nameBox.Text = elem.Name;
            }

            nameBox.VerticalAlignment = VerticalAlignment.Center;
            nameBox.HorizontalAlignment = fromMenuElement.tagAlignCentre ? HorizontalAlignment.Center : HorizontalAlignment.Left;
            imageParent.Children.Add(nameBox);
            Canvas.SetTop(nameBox, nametagOffset.Y);
            Canvas.SetLeft(nameBox, nametagOffset.X);

            return imageParent;
        }


    }
}
