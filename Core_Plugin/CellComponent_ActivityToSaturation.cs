using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using WPF_Chemotaxis;
using WPF_Chemotaxis.Model;
using WPF_Chemotaxis.Simulations;
using WPF_Chemotaxis.UX;

namespace Core_Plugin
{
    class CellComponent_ActivityToSaturation : LabelledLinkable, ICellComponent 
    {
        [Param (Name ="Min. Saturation")]
        public double minSaturation { get; set; } = 0.2;
        
        [Param(Name = "Max. Saturation")]
        public double maxSaturation { get; set; } = 1.0;

        [Param(Name = "Min. Activity")]
        public double minActivity { get; set; } = 0.01;

        [Param(Name = "Max. Activity")]
        public double maxActivity { get; set; } = 0.5;

        public void ConnectToCellType(CellType ct)
        {

        }

        public void Initialise(Simulation sim)
        {

        }

        public void ModifyDrawColour(Cell cell, Color base_primary, Color base_secondary, ref Color modified_primary, ref Color modified_secondary)
        {
            double mean = 0;
            double i = 0;
            foreach(ExpressionCoupler crr in cell.CellType.receptorTypes)
            {
                Receptor r = crr.ChildComponent as Receptor;
                if (r == null) continue;
                i++;
                mean += cell.ReceptorActivity(r);
            }
            if (i == 0) return;

            mean /= i;

            double val = (mean-minActivity)*(maxSaturation-minSaturation)/(maxActivity-minActivity) + minSaturation;
            val = Math.Min(maxActivity, Math.Max(minActivity, val));

            HSLColor hsl = HSLColor.FromRGB(modified_primary.R, modified_primary.G, modified_primary.B);

            HSLColor newHSL = new HSLColor(hsl.H, (float)val, hsl.L);

            modified_primary = newHSL.ToRGB();

            //System.Diagnostics.Debug.Print(string.Format("rgb_in::({0},{1},{2})->HSL::({3:0.00}{4:0.00}{5:0.00})     activity::{6:0.00}       HSL::HSL::({7:0.00}{8:0.00}{9:0.00})->rbg_out::({10},{11},{12})", base_primary.R, base_primary.G, base_primary.B, hsl.H, hsl.S, hsl.L, mean, newHSL.H, newHSL.S, newHSL.L, modified_primary.R, modified_primary.G, modified_primary.B));
        }

        public void Update(Cell cell, Simulation sim, WPF_Chemotaxis.Simulations.Environment env)
        {
           
        }
    }
}
