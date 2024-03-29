﻿using System;
using System.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using WPF_Chemotaxis.UX;
using ILGPU.IR.Values;
using System.Windows.Media.Imaging;
using WPF_Chemotaxis.Model;
using System.Diagnostics;

namespace WPF_Chemotaxis.VisualScripting
{
    internal class VSRelationElement : VSDiagramObject
    {

        VSDiagramObject primary;

        private List<VSLine> _lines = new();
        private bool separateModelPart;
        private Dictionary<VSDiagramObject, VSLine> _lineTargets = new();
        public IReadOnlyCollection<VSLine> RelationLines
        {
            get
            {
                return _lines;
            }
        }
        //public Line RelationLine { get; private set; }
        public ILinkable ModelRelation { get; private set; }
        private List<PropertyInfo> lineProps; 

        public VSRelationElement(Canvas canvas) : base(canvas)
        {

        }
        public VSRelationElement(VSDiagramObject primary, ILinkable modelRelation, bool separateModelPart, Canvas canvas) : base(canvas)
        {
            this.ModelRelation = modelRelation;
            this.separateModelPart = separateModelPart;
            this.primary = primary;
            Model.Model.Current.PropertyChanged += (s,e) => Redraw();
            lineProps = this.ModelRelation.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where((prop) => prop.GetCustomAttribute<VisualLineAttribute>() != null).ToList();
            if(separateModelPart) VSModelManager.Current.TryAdd(this, this.ModelRelation);
            Redraw();
        }

        private void Redraw()
        {
            CleanLines();
            MakeLines();
        }

        public override void Dispose()
        {
            CleanLines();
            base.Dispose();
        }
        private void MakeLines()
        {
            if (primary.Disposing)
            {
                this.Clean();
                return;
            }
            foreach (var prop in lineProps)
            {
                VisualLineAttribute vla = prop.GetCustomAttribute<VisualLineAttribute>();
                var lineLink = prop.GetValue(this.ModelRelation) as ILinkable;
                    
                if (lineLink != null)
                {
                    List<VSDiagramObject> lineTargetUIs = new List<VSDiagramObject>();
                    if (VSModelManager.Current.TryGetUIListFromLink(lineLink, out lineTargetUIs))
                    {
                        foreach (var lineTarget in lineTargetUIs)
                        {
                            if (_lineTargets.ContainsKey(lineTarget))
                            {
                                //System.Diagnostics.Debug.Print("SKIPPING LINE, ALREADY CREATED.");
                            }
                            else
                            {
                                Func<Color> lineColorFunc = ()=> Colors.SlateBlue;
                                var method = ModelRelation.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(m=>m.Name.Equals(vla.colorFunc));
                                Func<LineHeadType> lineheadFunc = () => LineHeadType.CIRCLE;
                                var headmethod = ModelRelation.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(m => m.Name.Equals(vla.parentArrowHeadFunc));

                                if (method != null && method.ReturnType==typeof(Color))
                                {
                                    lineColorFunc = (Func<Color>) Delegate.CreateDelegate(typeof(Func<Color>), ModelRelation, method);
                                }
                                if (headmethod != null && headmethod.ReturnType == typeof(LineHeadType))
                                {
                                    lineheadFunc = (Func<LineHeadType>) Delegate.CreateDelegate(typeof(Func<LineHeadType>), ModelRelation, headmethod);
                                }
                                else
                                {
                                    Trace.WriteLine(string.Format("Found no arrowhead method for link in {0}",lineLink.Name));
                                }
                                MakeLine(lineTarget, vla, lineheadFunc, lineColorFunc);
                            }
                        }
                    }
                    else
                    {
                        Trace.WriteLine(string.Format("No UI entry found for {0}", lineLink.Name));
                    }
                }
                else
                {
                    Trace.WriteLine(string.Format("lineLink was null- no iLinkable in the field {0} for {1}", prop.Name, ModelRelation.Name));
                }
            }
        }

        private void MakeLine(VSDiagramObject lineTarget, VisualLineAttribute annotation, Func<LineHeadType> linehead, Func<Color> colorFunc) 
        {
            VSLine newLine = new(this.primary, lineTarget, this._mainCanvas,
                                 annotation.parentAnchorDistance, annotation.childAnchorDistance,
                                 annotation.parentAnchor, annotation.childAnchor,
                                 linehead, annotation.childArrowHead, colorFunc);

            _lines.Add(newLine);
            _lineTargets.Add(lineTarget, newLine);
        }

        public override void Clean()
        {
            CleanLines();
            base.Clean();
        }

        public bool DuplicateWithNewHandle(VSDiagramObject oldHandle, VSDiagramObject newHandle, out VSRelationElement dupe)
        {
            if (!this.HasHandle(oldHandle))
            {
                dupe = null;
                return false;
            }
            if (oldHandle == this.primary) // i.e. if the primary was duplicated, we create a new relation object
            {
                dupe = new VSRelationElement(newHandle, this.ModelRelation, this.separateModelPart, _mainCanvas);
                return true;
            }
            else  // Otherwise, we add it to the lines needed here from the original primary...
            {
                Redraw();
                dupe = null;
                return false;
            }            
        }

        private void OnHandleMoved(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("Position") || e.PropertyName.Equals("Rotation"))
            {
                this.InvalidateVisual();
            }
        }
        public bool HasHandle(VSDiagramObject handle)
        {
            return (primary == handle || _lineTargets.Keys.Contains(handle));
        }
        private void CleanLines()
        {
            if (_lines == null)
            {
                _lines = new();
                _lineTargets = new();
                
            }
            else
            {
                
                foreach (var line in _lines)
                {
                    line.Clean();
                }
            }
            _lines.Clear();
            _lineTargets.Clear();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
        }
    }
}
