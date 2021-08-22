using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.IO;
using Newtonsoft.Json;
using System.Windows.Controls;
using System.Windows.Data;

namespace WPF_Chemotaxis.Model
{
    /// <summary>
    /// Class that links RegionRule lists with given colours in the environment PNG image.
    /// </summary>
    public class RegionType
    {
        public string Name { get; set; }

        private static RegionType currentlySelected = null;
        public static RegionType CurrentlySelected
        {
            get
            {
                return currentlySelected;
            }
            set
            {
                currentlySelected = value;

            }
        }
        
        //private Color color;
        //public Color Color { get { return color; }}

        [JsonProperty]
        private ObservableCollection<RegionRule> rules = new();

        /// <summary>
        /// The RegionRules currently associated with this RegionType.
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<RegionRule> Rules
        {
            get
            {
                return rules;
            }
        }
        public RegionType()
        {
            //this.color = clr;
        }

        /// <summary>
        /// Method for fixing a bug is RegionRule updates after loading a model from save/autosave. Shouldn't be accessed otherwise.
        /// </summary>
        public static void RehookAllUpdateEvents(ItemsControl control)
        {
            foreach(Color key in currentTypes.Keys)
            {
                foreach(RegionRule rule in currentTypes[key].rules)
                {
                    rule.OptionsUpdated += (s, e) => CollectionViewSource.GetDefaultView(control.ItemsSource).Refresh();
                }
            }
        }

        /// <summary>
        /// Adds rule to the rule list.
        /// </summary>
        public void AddRule(RegionRule rule)
        {

            rules.Add(rule);
        }

        /// <summary>
        /// Deletes rule index from the rule list, if possible.
        /// </summary>
        public void DeleteRuleAtIndex(int index)
        {
            if (index < 0 || index >= rules.Count) return;
            rules.RemoveAt(index);
        }



        #region Global region list stuff
        private static IDictionary<Color, RegionType> currentTypes = new Dictionary<Color, RegionType>();
        public static IEnumerable<KeyValuePair<Color, RegionType>> GetRegionTypes
        {
            get
            {
                return currentTypes;
            }
        }

        public static void ClearRegions()
        {
            currentTypes.Clear();
        }

        private static void TrimRegions()
        {

        }

        public static void AddRegionType(Color clr, RegionType rt)
        {
            if (clr == Colors.Black) return;

            if (currentTypes.ContainsKey(clr))
            {
                currentTypes[clr] = rt;
            }
            else
            {
                currentTypes.Add(clr, rt);
            }
        }
        public static RegionType GetRegionType(Color clr)
        {
            if (clr == Colors.Black) return null;
            RegionType type;
            if(currentTypes.TryGetValue(clr, out type))
            {
                return type;
            }
            else
            {
                type = new RegionType();
                type.Name = "New Region Type";
                currentTypes.Add(clr, type);
                return type;
            }
        }

        public static void SaveRegions(string path)
        {
            string file = JsonConvert.SerializeObject((Dictionary<Color, RegionType>)currentTypes, Formatting.Indented,
                   new JsonSerializerSettings
                   {
                       TypeNameHandling = TypeNameHandling.Auto,
                       ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                       PreserveReferencesHandling = PreserveReferencesHandling.All
                   });
            File.WriteAllText(path, file);
        }

        public static void LoadRegions(string path)
        {
            
            string fileRead = File.ReadAllText(path);
           
            try
            {
            currentTypes = JsonConvert.DeserializeObject<Dictionary<Color, RegionType>>(fileRead,
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        PreserveReferencesHandling = PreserveReferencesHandling.All
                    });
            }
            catch (JsonSerializationException e) { Console.WriteLine(e.Message); }

            foreach(Color key in currentTypes.Keys)
            {
                foreach(RegionRule rule in currentTypes[key].rules)
                {
                    rule.OnLoaded();
                }
            }
        }


        #endregion Global region list stuff
    }
}
