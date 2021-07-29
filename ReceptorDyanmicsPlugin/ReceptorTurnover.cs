using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Chemotaxis;
using WPF_Chemotaxis.Model;
using WPF_Chemotaxis.Simulations;
using WPF_Chemotaxis.UX;
using Newtonsoft.Json;

namespace ReceptorDyanmicsPlugin
{
    class ReceptorTurnover : LabelledLinkable, ICellComponent
    {
        [JsonProperty]
        [InstanceChooser(label = "Receptor")]
        public Receptor Input;

        [Param(Name = "Basal expression", Min = 0, Max = 1)]
        public double rate { get; set; } = 0;

        [Param(Name = "Bound internalisation rate", Min = 0, Max = 1)]
        public double min_input { get; set; } = 0;

        private Dictionary<Cell, double> expressionMultipliers;

        private Simulation.CellNotificationHandler RegisterCell;

        public void Initialise(Simulation sim)
        {
            RegisterCell = (sim, cell, args) => RegisterNewCell(sim, cell);
            sim.CellAdded += RegisterCell;
        }

        public void Update(Cell cell, Simulation sim, WPF_Chemotaxis.Simulations.Environment env, IFluidModel flow)
        {
            throw new NotImplementedException();
        }

        private void RegisterNewCell(Simulation sim, Cell cell)
        {

        }
    }
}
