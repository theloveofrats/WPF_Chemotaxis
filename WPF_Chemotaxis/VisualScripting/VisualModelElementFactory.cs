using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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

        /// <summary>
        /// Creates a UI element appropriate to a specified type IF the type is decorated with an appropriate VSElement, and returns it via out param. Otherwise, returns false with a null out param.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="psn"></param>
        /// <param name="manager"></param>
        /// <param name="createdElement"></param>
        /// <param name="LeftMouseDownHandler"></param>
        /// <param name="LeftMouseUpHandler"></param>
        /// <returns></returns>
        public bool TryCreateUIForExtantModelElement(ILinkable linkToConnect, Point psn, out VSDiagramObject createdElement)
        {
            var vsAttribute = linkToConnect.GetType().GetCustomAttribute<VSElementAttribute>();
            if (vsAttribute != null)
            {
                VSListMenuElement virtualMenuItem = new VSListMenuElement(vsAttribute, linkToConnect.GetType());
                createdElement = new VSUIElement(virtualMenuItem, psn, linkToConnect, targetCanvas);
                Trace.WriteLine(String.Format("Made visual for {0} in reaction to model change", linkToConnect.Name));
                return true;
            }
            else
            {
                createdElement = null;
                return false;
            }
        }

        /// <summary>
        /// Creates a UI element from a given menu selection fromMenuElement.
        /// </summary>
        /// <param name="fromMenuElement"></param>
        /// <param name="clickPsn"></param>
        /// <param name="vsModelManager"></param>
        /// <param name="LeftMouseDownHandler"></param>
        /// <param name="LeftMouseUpHandler"></param>
        /// <returns></returns>
        /*public UIElement CreateModelElementImage(VSListMenuElement fromMenuElement, Point clickPsn, ILinkable linkedModelElement, Action<object, MouseButtonEventArgs> LeftMouseDownHandler, Action<object, MouseButtonEventArgs> LeftMouseUpHandler, Action<object, MouseButtonEventArgs> RightMouseUpHandler)
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
            img.MouseLeftButtonDown += (o, e) => LeftMouseDownHandler(o,e);
            img.MouseLeftButtonUp   += (o, e) => LeftMouseUpHandler(o,e);
            img.MouseRightButtonUp  += (o, e) => RightMouseUpHandler(o, e);

            TextBox nameBox = new TextBox();
            Binding myTargetBinding = new Binding("Name");
            myTargetBinding.Source = linkedModelElement;
            myTargetBinding.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(nameBox, TextBox.TextProperty, myTargetBinding);
            nameBox.Text = linkedModelElement.Name;

            nameBox.VerticalAlignment = VerticalAlignment.Center;
            nameBox.HorizontalAlignment = fromMenuElement.tagAlignCentre ? HorizontalAlignment.Center : HorizontalAlignment.Left;
            imageParent.Children.Add(nameBox);
            Canvas.SetTop(nameBox, nametagOffset.Y);
            Canvas.SetLeft(nameBox, nametagOffset.X);

            return img;
        }*/


    }
}
