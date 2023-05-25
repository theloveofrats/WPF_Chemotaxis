using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Windows;
using WPF_Chemotaxis.Model;
using WPF_Chemotaxis.Simulations;
using WPF_Chemotaxis.UX;

namespace WPF_Chemotaxis.MitogensPlugin
{
    public class CellComponent_Mitosis: LabelledLinkable, ICellComponent
    {
        private string name = "Mitosis";                                           
        public override string Name {                                                               
            get {                                                                                   
                return name;
            }
            set
            {
                name = value;
            }
        }
        private SciRand rnd = new();

        [JsonIgnore]                                                                                
        IDictionary<Cell, MitosisParams> paramRefs = new Dictionary<Cell, MitosisParams>();               
                                                                                                    
                                                                                                    
        public override string DisplayType => "Mitosis logic";                                

        [Param(Name = "Mean time (mins)", Min = 0)]                                             
        public CenteredDoubleRange meanTimeRange { get; set; } = new CenteredDoubleRange(720,0);     
                                                                                                    
        [Param(Name = "Steepness (chance/min)", Min = 0)]                                              
        public CenteredDoubleRange steepnessRange { get; set; } = new CenteredDoubleRange(0.01,0);

       
        

        public virtual void Initialise(Simulation sim)                                              
        {                                                                                           
            paramRefs.Clear();                                                                      
            this.PropertyChanged += (s, e) => UpdateParams(this, sim);                              
            sim.CellAdded += (s, e) => this.RegisterCell(s, e.NewCell);
        }

        private void RegisterCell(Simulation sim, Cell cell)                                        
        {
            if (cell.CellType.components.Contains(this))
            {
                lock (paramRefs)                                                                        
                {                                                                                       
                    if (!paramRefs.ContainsKey(cell))
                    {
                        MitosisParams par = new MitosisParams(cell.Id);
                        par.Update(this, sim);
                        paramRefs.Add(cell, par);
                    }
                }
            }
        }

        private void UpdateParams(CellComponent_Mitosis component, Simulation sim)                             
        {                                                                                           
            foreach(Cell key in paramRefs.Keys)
            {
                paramRefs[key].Update(component, sim);
            }
        }


        public virtual void Update(Cell cell, Simulations.Simulation sim, Simulations.Environment env)       // Update is called every time-step and is where the core logic goes. 
        {

            MitosisParams mitParams;
            if (paramRefs.TryGetValue(cell, out mitParams)){
                mitParams.Update(sim.Settings.dt);
                if (mitParams.ReadyToSplit())
                {
                    sim.AddCell(
                                cell.CellType, cell.X + 2.0 * cell.radius * (rnd.value - 0.5),
                                cell.Y + 2.0 * cell.radius * (rnd.value - 0.5),
                                CellEventType.MITOTIC
                            );
                    mitParams.ConfirmSplit();
                }
            }
        }                                                                                                                     
                                                                                                                              
    }
}
