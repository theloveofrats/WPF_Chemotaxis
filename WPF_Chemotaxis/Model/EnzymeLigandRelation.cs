﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Chemotaxis.UX;
using WPF_Chemotaxis.Simulations;
using System.Windows;
using Newtonsoft.Json;
using WPF_Chemotaxis.VisualScripting;

namespace WPF_Chemotaxis.Model
{
    /// <summary>
    /// Relational class for cell-ligand direct interactions (rather than receptor-mediated interactions).
    /// </summary>

    [VSRelationAttribute(forcedPositionType = ForcedPositionType.NONE, childFieldName = "enzyme", parentFieldName = "_inputLigand")]
    public class EnzymeLigandRelation : LabelledLinkable
    {
        [JsonProperty]
        [Link]
        CellSurfaceEnzyme enzyme;

        [JsonProperty]
        [InstanceChooser(label = "Input ligand")]
        private Ligand _inputLigand;
        public Ligand Ligand
        {
            get {
                return _inputLigand;
            }
        }

        [JsonProperty]
        [InstanceChooser(label = "Product ligand")]
        private Ligand _outputLigand;
        public Ligand ProductLigand
        {
            get
            {
                return _outputLigand;
            }
        }


        //DEBUG REMOVE!
        Random random = new Random();

        [Param(Name = "Input:Output")]
        public double multiplier { get; set; } = 1;
        [Param(Name = "vMax")]
        public double vMax { get; set; } = 10;
        [Param(Name = "Hill Coeficient")]
        public double Hill { get; set; } = 1;
        [Param(Name = "kM")]
        public double kM { get; set; } = 0.5;

        public EnzymeLigandRelation() { }

        public EnzymeLigandRelation(CellSurfaceEnzyme enzyme, Ligand ligand) : base()
        {
            this.enzyme = enzyme;
            this._inputLigand = ligand;
            Init();
        }

        public void SetLigand(Ligand newLigand)
        {
            this._inputLigand = newLigand;
        }
        public void SetProduct(Ligand newLigand)
        {
            this._outputLigand = newLigand;
        }

        public override void RemoveElement(ILinkable element, ILinkable replacement = null)
        {
            if (element is Ligand)
            {
                Ligand l = (Ligand)element;
                if (this._inputLigand == l)
                {
                    this._inputLigand = (Ligand)replacement;
                }
                if (this._outputLigand == l)
                {
                    this._outputLigand = (Ligand)replacement;
                }
                if (this._inputLigand == null)
                {
                    Model.Current.RemoveElement(this);
                }
            }
            else if (element is CellSurfaceEnzyme)
            {
                CellSurfaceEnzyme enz = (CellSurfaceEnzyme)element;
                if (this.enzyme == enz)
                {
                    this.enzyme = (CellSurfaceEnzyme)replacement;

                    if (this.enzyme == null) Model.Current.RemoveElement(this);
                }
            }
        }

        [JsonProperty]
        private string name;
        [JsonIgnore]
        public override string Name
        {
            get
            {
                if (name != null) return name;
                else if (enzyme == null || (Ligand == null && _outputLigand == null)) return "Broken Enzyme-Ligand interaction";
                else if (_outputLigand == null) return string.Format("New {0}-{1} interaction", enzyme.Name, _outputLigand.Name);
                return string.Format("New {0}->{1}->{2} reaction", Ligand.Name, enzyme.Name, _outputLigand.Name);
            }
            set
            {
                name = value;
            }
        }
    }
}