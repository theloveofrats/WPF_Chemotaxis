using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Serialization;
using System.Threading.Tasks;
using WPF_Chemotaxis.UX;
using Newtonsoft.Json;
using Microsoft.Win32;
using System.IO;

namespace WPF_Chemotaxis.Model
{
    /// <summary>
    /// Root class for navigating elements of the theoretical model. Is currently responsible both for 
    /// being the access point for the model, and controlling its navigation. This should probably be split
    /// into two classes.
    /// </summary>
    public class Model : ILinkable
    {
        [Link]
        private static string label;

        private static Model current;
        public static Model Current
        {
            get
            {
                return current;
            }
        }
        private ObservableCollection<ILinkable> masterElementList = new();

        private bool freezeAdditions;
        public static bool FreezeAdditions
        {
            get
            {
                return current.freezeAdditions;
            }
            internal set
            {
                current.freezeAdditions = value;
            }
        }
        public static ObservableCollection<ILinkable> MasterElementList
        {
            get
            {
                return current.masterElementList;
            }
        }

        private static List<ILinkable> focusHistory = new();

        private static int listFocus = 0;

        public Model()
        {
            current = this;
            focusHistory.Add(this);
        }

        public Model(string fromFile)
        {

        }

        public void RemoveElement(ILinkable element, ILinkable replacement=null)
        {
            if(this.masterElementList.Contains(element)) this.masterElementList.Remove(element);

            List<ILinkable> temp = masterElementList.ToList();

            foreach(ILinkable link in temp)
            {
                link.RemoveElement(element, replacement);
            }
        }

        public static ILinkable CurrentFocus
        {
            get
            {
                return focusHistory[listFocus];
            }
        }

        public static ILinkable FocusBack
        {
            get
            {
                if (listFocus > 0) return focusHistory[--listFocus];
                else return focusHistory[0];
            }
        }
        public static ILinkable FocusForward
        {
            get
            {
                if (listFocus < focusHistory.Count - 1) listFocus++;
                return focusHistory[listFocus];
            }
        }

        public static void Reset()
        {
            listFocus = 0;
            focusHistory = new List<ILinkable>() {focusHistory[0]};
        }

        public static void SetNextFocus(ILinkable next)
        {
            focusHistory = focusHistory.GetRange(0, listFocus+1);
            focusHistory.Add(next);
            listFocus++;
        }

        string ILinkable.Name { get => "All Elements"; set => label=value;}

        string ILinkable.DisplayType => "Model Structure";

        ObservableCollection<ILinkable> ILinkable.LinkList => MasterElementList;

        [ElementAdder(label = "Cell", type =typeof(CellType))]
        public void AddCell(CellType cell)
        {
            CellType.cellTypes.Add(cell);
        }

        [ElementAdder(label="Ligand",type =typeof(Ligand))]
        public void AddLigand(Ligand ligand)
        {

        }

        [ElementAdder(label = "Receptor", type = typeof(Receptor))]
        public void AddLigand(Receptor receptor)
        {

        }

        public ObservableCollection<UIParameterLink> ParamList
        {
            get
            {
                return new();
            }
        }

        public ObservableCollection<UIOptionLink> OptsList
        {
            get
            {
                return new();
            }
        }

        public void SerializeModel()
        {
            //Dialogs should be refactored out of this class, really.
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.DefaultExt = "json";
            saveFileDialog.AddExtension = true;
            saveFileDialog.Filter = "JSON | *.json";
            if (saveFileDialog.ShowDialog() == true)
            {
                SerializeModelToPath(saveFileDialog.FileName);
            }
        }
        public void SerializeModelToPath(string path)
        {
            string file = JsonConvert.SerializeObject(masterElementList, Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects
                    });
            File.WriteAllText(path, file);
        }


        public void DeserializeModelAtPath(string path, bool clean)
        {
            string fileRead = File.ReadAllText(path);

            if (clean)
            {

                try
                {
                    List<ILinkable> newElements = JsonConvert.DeserializeObject<List<ILinkable>>(fileRead,
                            new JsonSerializerSettings
                            {
                                TypeNameHandling = TypeNameHandling.Auto,
                                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                                PreserveReferencesHandling = PreserveReferencesHandling.Objects
                            });
                    foreach (ILinkable link in newElements)
                    {
                        if (!masterElementList.Contains(link)) masterElementList.Add(link);
                    }
                }
                catch (JsonSerializationException e) { Console.WriteLine(e.Message); }
            }
            else
            {
                try
                {
                    List<ILinkable> newElements = JsonConvert.DeserializeObject<List<ILinkable>>(fileRead,
                            new JsonSerializerSettings
                            {
                                TypeNameHandling = TypeNameHandling.Auto,
                                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                                    //ReferenceResolverProvider = () => new ExtantReferenceResolver(masterElementList)
                                });

                    foreach (ILinkable link in newElements)
                    {
                        if (!masterElementList.Contains(link)) masterElementList.Add(link);
                    }
                }
                catch (JsonSerializationException e) { Console.WriteLine(e.Message); }
            }
        }

        /*public void DeserializeModel(bool clean)
        {
            if (clean) masterElementList.Clear();

            //Dialogs should be refactored out of this class, really.
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.DefaultExt = "json";
            dialog.AddExtension = true;
            dialog.Filter = "JSON | *.json";
            if (dialog.ShowDialog() == true)
            {
                if (!File.Exists(dialog.FileName)) return;
                DeserializeModelAtPath(dialog.FileName, clean);
            }
        }*/
    }
}
