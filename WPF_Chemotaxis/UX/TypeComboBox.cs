using System;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPF_Chemotaxis.UX
{
    /// <summary>
    /// Interaction logic for TypeComboBox.xaml
    /// </summary>
    public partial class TypeComboBox : ComboBox
    {
        private Type type;
        private Type selectedType;
        private ObservableCollection<Type> cachedTypes = new();
        public Type SelectedType
        {
            get
            {
                return selectedType;
            }
        }

        public TypeComboBox()
        {
            //InitializeComponent();
            this.SelectionChanged += (o, e) => this.selectedType = cachedTypes[this.SelectedIndex];
        }

        static TypeComboBox()
        {

        }

        public void SetType(Type type)
        {
            this.type = type;
            cachedTypes.Clear();
            foreach (Type t in

                (AppDomain.CurrentDomain.GetAssemblies()
                               .SelectMany(s => s.GetTypes())
                               .Where(p => type.IsAssignableFrom(p) && !p.IsAbstract && !p.IsInterface)))
            {
                cachedTypes.Add(t);
            }
            this.ItemsSource = from Type t in cachedTypes select t.Name;
            this.selectedType = cachedTypes[this.SelectedIndex];
        }
    }
}
