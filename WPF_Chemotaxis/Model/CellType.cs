using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Chemotaxis.UX;
using Newtonsoft.Json.Serialization;
using WPF_Chemotaxis.VisualScripting;
using System.Windows.Media;

namespace WPF_Chemotaxis.Model
{
    /// <summary>
    /// Class for storing paramaters for a cell type that can be shared by many cells
    /// and exposed to the UI for modification.
    /// </summary>

    [VSElement(ui_TypeLabel="Cell", symbolResourcePath = "Resources/CircleIcon.png", symbolSize = 25.0, tagX =-40, tagY = -50, tagCentre = true)] 
    public class CellType : LabelledLinkable
    {
        public string label = "New Cell Type"; 
        [Param(Name = "Base speed", Min = 0)]
        public CenteredDoubleRange baseSpeed { get; set; } = new CenteredDoubleRange(12.5,2);

        [Param(Name = "Radius (um)", Min = 0)]
        public CenteredDoubleRange radius { get; set; } = new CenteredDoubleRange(6,0);

        public static List<CellType> cellTypes { get; private set; } = new();

        [Link]
        public List<CellReceptorRelation> receptorTypes { get; private set; } = new();
        [Link]
        public List<CellLigandRelation> ligandInteractions { get; private set; } = new();

        [Link(overrideName = "Movement logic")]
        public List<ICellComponent> components { get; private set;  } = new();

        [Link(overrideName = "Draw handler")]
        [ClassChooser(label = "Draw handler", baseType = typeof(ICellDrawHandler))]
        public ICellDrawHandler drawHandler { get; private set; }

        public void SetDrawHandler(ICellDrawHandler drawHandler)
        {
            this.drawHandler = drawHandler;
        }

        public CellType() : base()
        {
            Init();
        }
        public CellType(string label) : base(label)
        {
            Init();
        }

        [ElementAdder(label ="Add Receptor", type = typeof(Receptor))]
        public void AddReceptorType(Receptor receptor)
        {
            foreach(CellReceptorRelation crr in receptorTypes)
            {
                if (crr.Receptor == receptor) return; //Already have this receptor in the relation list.
            }

            CellReceptorRelation relation = new CellReceptorRelation(this, receptor); 
            if(!receptorTypes.Contains(relation)) receptorTypes.Add(relation);                        

        }

        [ElementAdder(label = "Add Ligand Interaction", type = typeof(Ligand))]
        public void AddLigandInteraction(Ligand ligand)
        {
            CellLigandRelation relation = new CellLigandRelation(this, ligand);
            if (!ligandInteractions.Contains(relation)) ligandInteractions.Add(relation);
        }
        
        public override void RemoveElement(ILinkable element, ILinkable replacement = null)
        {
            //System.Diagnostics.Debug.Print(string.Format("Trying to remove element {0} from {1}", element.Name, this.Name));
            if (element == null) return;
            if(element is Receptor)
            {
                List<CellReceptorRelation> drop = (from r in receptorTypes where r.Receptor == element select r).ToList();
                foreach (var receptorType in drop)
                {
                    Model.Current.RemoveElement(receptorType, replacement);
                }
            }

            else if (element is CellLigandRelation)
            {
                if (this.ligandInteractions.Contains(element))
                {
                    this.ligandInteractions.Remove((CellLigandRelation)element);
                    if (replacement != null && replacement.GetType().IsAssignableTo(typeof(CellLigandRelation)))
                    {
                        this.ligandInteractions.Add((CellLigandRelation)replacement);
                    }
                }
            }
            else if(element is CellReceptorRelation)
            {
                if (this.receptorTypes.Contains(element))
                {
                    this.receptorTypes.Remove((CellReceptorRelation)element);
                    if (replacement != null && replacement.GetType().IsAssignableTo(typeof(CellReceptorRelation)))
                    {
                        this.receptorTypes.Add((CellReceptorRelation)replacement);
                    }
                }
            }
            else if (element.GetType().IsAssignableTo(typeof(ICellComponent)))
            {
                if (this.components.Contains((ICellComponent) element))
                {
                    this.components.Remove((ICellComponent)element);
                    if (replacement != null && replacement.GetType().IsAssignableTo(typeof(ICellComponent)))
                    {
                        this.components.Add((ICellComponent)replacement);
                    }
                }
            }
        }

        [ElementAdder(label = "Add Logic Component", type = typeof(ICellComponent))]
        public void AddCellLogicComponent(ICellComponent component)
        {
            if (component.GetType().IsAssignableTo(typeof(ILinkable)))
            {
                ILinkable link = (ILinkable)component;
                Model.Current.AddElement(link);
            }
            this.components.Add(component);
            component.ConnectToCellType(this);
        }
    }
}
