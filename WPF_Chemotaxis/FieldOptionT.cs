using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WPF_Chemotaxis
{
    //Shorthand for T is a number here...
    public class FieldOption<T> where T: IConvertible, IComparable<T>, IEquatable<T>
    {
        public T currentValue { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        public void OpenPropField(MouseButtonEventArgs e)
        {
            Popup pop = new Popup();
            pop.StaysOpen = true;
            pop.Placement = PlacementMode.MousePoint;
            pop.HorizontalOffset = 4;
            pop.VerticalOffset = 16;
            pop.MouseLeave += (s, e) => { 
                pop.IsOpen = false;            
            };

            TextBox box = new TextBox();
            box.FontSize = 18;
            box.MinWidth = 80;
            box.Background = new SolidColorBrush(Colors.White);

            box.Text = currentValue.ToString();
            pop.Child = box;
            box.KeyUp += (s, e) =>
             {
                 if (e.Key == Key.Return || e.Key == Key.Enter)
                 {
                     T result;
                     if (TryParseText(box.Text, out result))
                     {
                         currentValue = result;
                         pop.IsOpen = false;
                         NotifyPropertyChanged("value");
                     }
                     else
                     {
                         ColorAnimation ca = new ColorAnimation();
                         ca.From = Colors.White;
                         ca.To = Colors.Red;
                         ca.AutoReverse = true;
                         ca.RepeatBehavior = new RepeatBehavior(3);
                         ca.Duration = new System.Windows.Duration(TimeSpan.FromSeconds(0.09));

                         box.Background.BeginAnimation(SolidColorBrush.ColorProperty, ca);
                     }
                 }
             };

            pop.IsOpen = true;
        }

        private bool TryParseText(string textInput, out T result)
        {
            double number;
            bool res = Double.TryParse(textInput, out number);
            if (res)
            {
                result = (T) Convert.ChangeType(number, typeof(T));
                return true;
            }
            result = default(T);
            return false;
        }
    }
}
