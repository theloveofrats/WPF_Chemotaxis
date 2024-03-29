﻿using System;
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
using System.Collections.Specialized;
using System.ComponentModel;

namespace WPF_Chemotaxis.Model
{
    /// <summary>
    /// Root class for navigating elements of the theoretical model. Is currently responsible both for 
    /// being the access point for the model, and controlling its navigation. This should probably be split
    /// into two classes.
    /// </summary>
    public class Model : ILinkable, INotifyPropertyChanged
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
        public static ReadOnlyCollection<ILinkable> MasterElementList
        {
            get
            {
                return new ReadOnlyCollection<ILinkable>(current.masterElementList);
            }
        }

        public void Clear()
        {
            masterElementList.Clear();
        }

        private static List<ILinkable> focusHistory = new();

        private int listFocus = 0;
        private int ListFocus 
        { 
            get
            {
                return listFocus;
            } 
            set
            {

                listFocus = value;
                OnFocusModelElement();
            }
        }

        public event EventHandler<NotifyCollectionChangedEventArgs> ModelChanged;
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<EventArgs> FocusModelElement;

        private void OnModelChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (ModelChanged != null)
            {
                ModelChanged(sender, e);
            }
        }

        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

        private void OnFocusModelElement()
        {
            FocusModelElement?.Invoke(this, new EventArgs());
        }

        public Model()
        {
            current = this;
            focusHistory.Add(this);
            masterElementList.CollectionChanged += OnModelChanged;
        }

        public void AddElement(ILinkable element)
        {
            if (this.masterElementList.Contains(element)) return;
            masterElementList.Add(element);

            // All elements should tell the main model if they update parameters. That way,
            // anything that needs to know about changes, like the VS system, can be notified.
            // If the element doesn't implement INotifyPropertyChanged, it's probably not 
            // relevant to watch it!
            var castEl = (element as INotifyPropertyChanged);
            if(castEl!=null) castEl.PropertyChanged += this.NotifyModelElementPropertyChanged;        
        }

        private void NotifyModelElementPropertyChanged(object source, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(source, e);
        }

        public void RemoveElement(ILinkable element, ILinkable replacement=null)
        {
            var castEl = (element as INotifyPropertyChanged);
            if (castEl != null) castEl.PropertyChanged -= this.NotifyModelElementPropertyChanged;

            if (this.masterElementList.Contains(element)) this.masterElementList.Remove(element);
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
                return focusHistory[Current.ListFocus];
            }
        }

        public static ILinkable FocusBack
        {
            get
            {
                if (Current.ListFocus > 0) return focusHistory[--Current.listFocus];
                else return focusHistory[0];
            }
        }
        public static ILinkable FocusForward
        {
            get
            {
                if (Current.ListFocus < focusHistory.Count - 1) Current.listFocus++;
                return focusHistory[Current.ListFocus];
            }
        }

        public static void Reset()
        {
            Current.ListFocus = 0;
            focusHistory = new List<ILinkable>() {focusHistory[0]};
        }

        public static void SetNextFocus(ILinkable next)
        {
            focusHistory = focusHistory.GetRange(0, Current.ListFocus +1);
            focusHistory.Add(next);
            Current.ListFocus++;
        }

        string ILinkable.Name { get => "All Elements"; set => label=value;}

        string ILinkable.DisplayType => "Model Structure";

        ObservableCollection<ILinkable> ILinkable.LinkList => masterElementList;

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
        public void AddReceptor(Receptor receptor)
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

        public bool TryAddTo(ILinkable other)
        {
            return false;
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
