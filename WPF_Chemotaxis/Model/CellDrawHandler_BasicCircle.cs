using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WPF_Chemotaxis.Simulations;
using WPF_Chemotaxis.UX;

namespace WPF_Chemotaxis.Model
{
    /// <summary>
    /// A cell drawing class that draws a circle of appropriate radius at the position of the cell.
    /// </summary>
    class CellDrawHandler_BasicCircle : LabelledLinkable, ICellDrawHandler
    {

        [Param(Name = "Base Color")]
        public Color baseColor { get; set; } = Colors.DarkCyan;

        [Param(Name = "2nd color")]
        public Color color2 { get; set; } = Colors.Black;

        public CellDrawHandler_BasicCircle() : base()
        {
            Init();
        }

        public void Draw(Cell cell, Simulations.Environment env, WriteableBitmap targetBmp, ICollection<Point> occupiedPoints)
        {
            Color modPrimary = baseColor;
            Color modSecondary = color2;
            foreach(ICellComponent comp in cell.CellType.components)
            {
                comp.ModifyDrawColour(cell, baseColor, color2, ref modPrimary, ref modSecondary);
            }

            double dx = env.settings.DX;

            double scale = targetBmp.PixelWidth*1.0/env.Width;

            WriteableBitmapExtensions.FillEllipseCentered(targetBmp, (int)Math.Round(scale * cell.X / dx), (int)Math.Round(scale * cell.Y / dx), (int)Math.Round(scale * cell.radius / dx), (int)Math.Round(scale * cell.radius / dx), modPrimary);
            WriteableBitmapExtensions.DrawEllipseCentered(targetBmp, (int)Math.Round(scale * cell.X / dx), (int)Math.Round(scale * cell.Y / dx), (int)Math.Round(scale * cell.radius / dx), (int)Math.Round(scale * cell.radius / dx), modSecondary);
        }

        public void Initialise(Simulation sim)
        {
            
        }
    }
}
