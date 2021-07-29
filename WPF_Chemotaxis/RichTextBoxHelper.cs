using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace WPF_Chemotaxis
{
    public class RichTextBoxHelper : DependencyObject
    {
        public static FlowDocument GetDependencyDocument(DependencyObject obj)
        {
            return (FlowDocument) obj.GetValue(DependencyDocumentProperty);
        }

        public static void SetDependencyDocument(DependencyObject obj, string value)
        {
            obj.SetValue(DependencyDocumentProperty, value);
        }

        public static readonly DependencyProperty DependencyDocumentProperty =
            DependencyProperty.Register(
                "DependencyDocument",
                typeof(FlowDocument),
                typeof(RichTextBoxHelper),
                new PropertyMetadata(new PropertyChangedCallback(DocumentChanged)));

        private static void DocumentChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("RTB document changed");
        }
    }
}