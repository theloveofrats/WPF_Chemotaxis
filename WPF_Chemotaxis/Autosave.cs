using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using WPF_Chemotaxis.Model;
using WPF_Chemotaxis.UX;

namespace WPF_Chemotaxis
{
    public class Autosave : IDisposable
    {
        public List<ILinkable> ModelElements { get; set; }
        public Dictionary<Color, RegionType> Regions { get; set; }
        public string mazePath { get; set; }

        public string saveDirPath { get; set; }

        public MiscParamTable miscParams { get; set; }

        public void Dispose()
        {
            ModelElements.Clear();
            Regions.Clear();
            ModelElements = null;
            Regions = null;
            mazePath = null;
        }

        public void WriteToFile(string path)
        {

        }

        public static Autosave ReadFromFile(string path)
        {
            Trace.WriteLine("******* SAVE FILE LOADING *******");
            Trace.WriteLine(" ");
            string fileRead = File.ReadAllText(path);
            Trace.WriteLine(String.Format("Opening save at {0}", path));
            try
            {
                Autosave autosave = JsonConvert.DeserializeObject<Autosave>(fileRead,
                        new JsonSerializerSettings
                        {
                            TypeNameHandling = TypeNameHandling.Auto,
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                            PreserveReferencesHandling = PreserveReferencesHandling.All
                        });
                return autosave;
            }
            catch (JsonSerializationException e) {
                Trace.WriteLine("JsonConvert failed to read save!");
                Trace.WriteLine(e.Message);
                return null;
            }
        }
    }
}
