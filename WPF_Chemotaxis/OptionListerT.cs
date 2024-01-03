using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using WPF_Chemotaxis.UX;

namespace WPF_Chemotaxis
{
    public class OptionLister<T> where T : ILinkable
    {
        private List<T> opts = new();
        public T Selected { get; set; }

        public string SelectedLabel
        {
            get
            {
                if (Selected == null)
                {
                    if (hasDefault) return defaultLabel;

                    else if (opts.Count > 0)
                    {
                        Selected = opts[0];
                        return Selected.Name;
                    }
                    else return "";
                }
                else return Selected.Name;
            }
        }

        private bool hasDefault = false;
        private string defaultLabel = "";

        public event PropertyChangedEventHandler PropertyChanged;

        public OptionLister(bool hasDefault,string defaultLabel) 
        {
            this.hasDefault = hasDefault;
            this.defaultLabel = defaultLabel;
        }

        public T[] RandomDraw(int numberOfDraws)
        {
            T[] results = new T[numberOfDraws];
            SciRand rnd = new SciRand();

            if (opts.Count == 0)
            {
                GenerateOptions(Model.Model.MasterElementList);
            }
            if (opts.Count == 0) return results;

            for(int i=0; i<numberOfDraws; i++)
            {
                results[i] = rnd.RandomElement<T>(opts);
            }

            return results;
        }

        public void ShowOptionsPopup(MouseButtonEventArgs e)
        {
            Popup pop = new Popup();
            pop.StaysOpen = true;
            pop.Placement = PlacementMode.MousePoint;
            ItemsControl items = new ItemsControl();
            pop.Child = items;
            items.ItemsSource = GenerateButtons(pop);

            pop.MouseLeave += (s, e) => pop.IsOpen = false;
            pop.IsOpen = true;
        }

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private IList<Button> GenerateButtons(Popup popup)
        {
            IList<Button> buttons = new List<Button>();
            Button btn;
            if (hasDefault) {
                btn = new Button();
                btn.Content = defaultLabel;
                btn.Click += (s, e) =>
                {
                    Selected = default(T);
                    NotifyPropertyChanged(btn.Name);
                    popup.IsOpen = false;
                };
                buttons.Add(btn);
            }

            for (int i = 0; i < opts.Count; i++)
            {
                int sel = i;
                btn = new Button();
                btn.Content = opts[i].Name;
                btn.Click += (s, e) =>
                {
                    Selected = opts[sel];
                    //FireUpdate(btn, new EventArgs());
                    NotifyPropertyChanged(btn.Name);
                    popup.IsOpen = false;
                };
                buttons.Add(btn);
            }
            return buttons;
        }

        public void GenerateOptions(ICollection<ILinkable> masterList)
        {
            opts.Clear();
            foreach (ILinkable link in masterList)
            {
                if (link is T)
                {
                    opts.Add((T)link);
                }
            }
        }
    }
}
