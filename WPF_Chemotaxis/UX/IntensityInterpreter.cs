using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WPF_Chemotaxis.UX
{
    class IntensityInterpreter
    {
        private const int NUMBER_OF_COLOR_POINTS = 256;
        private LinearGradientBrush lut;
        private ComboBox cbLUT;
        private Color[] colors = new Color[NUMBER_OF_COLOR_POINTS];

        public IntensityInterpreter(ComboBox cbLUT)
        {
            this.cbLUT = cbLUT;
            cbLUT.SelectionChanged += updateLUT;
            updateLUT(cbLUT, new EventArgs());
        }

        private void updateLUT(object sender, EventArgs e)
        {
            this.lut = (LinearGradientBrush) cbLUT.SelectedItem;
            UpdateColorLookup();
        }

        private void UpdateColorLookup()
        {
            double loc;
            for(int i=0; i<NUMBER_OF_COLOR_POINTS; i++)
            {
                loc = (1.0 * i) / NUMBER_OF_COLOR_POINTS;
                colors[i] = GetLUTColorAtPoint(loc);
            }
        }

        public Color IntensityToColor(byte intensity)
        {
            return colors[intensity];
        }

        private Color GetLUTColorAtPoint(double loc)
        {
            // Clamp to range;
            loc = Math.Clamp(loc, lut.GradientStops.Min(gs => gs.Offset), lut.GradientStops.Max(gs => gs.Offset));
            
            //Get stops above/below;
            GradientStop gs0 = this.lut.GradientStops.Where(gs => gs.Offset <= loc).OrderBy(gs=>gs.Offset).Last();
            GradientStop gs1 = this.lut.GradientStops.Where(gs => gs.Offset >= loc).OrderBy(gs => gs.Offset).First();

            //If we are exactly on a stop, return stop colour.
            if (gs0 == gs1) return gs0.Color;

            //Otherwise, lerp a colour
            double lerpval = (loc - gs0.Offset) / (gs1.Offset - gs0.Offset);

            return Lerp(gs0.Color, gs1.Color, lerpval);
        }

        //Should be an extension method, probably move this.
        private Color Lerp(Color _self, Color towards, double val)
        {
            val = Math.Clamp(val, 0.0, 1.0);

            Color newColor = new Color();

            newColor.A = (byte)Math.Round((1.0 - val) * _self.A + val * towards.A);
            newColor.R = (byte)Math.Round((1.0 - val) * _self.R + val * towards.R);
            newColor.G = (byte)Math.Round((1.0 - val) * _self.G + val * towards.G);
            newColor.B = (byte)Math.Round((1.0 - val) * _self.B + val * towards.B);

            return newColor;
        }
    }
}
