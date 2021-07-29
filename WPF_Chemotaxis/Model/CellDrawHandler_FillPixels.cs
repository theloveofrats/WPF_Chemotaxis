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
    /// A cell drawing class that fills in the pixels in the enironmentgrid that the cell can interact with / sample from.
    /// </summary>
    class CellDrawHandler_FillPixels : LabelledLinkable, ICellDrawHandler
    {
        [Param(Name = "Base Color")]
        public Color baseColor { get; set; } = Color.FromArgb(255, 150, 150, 150);

        [Param(Name = "2nd color")]
        public Color color2 { get; set; } = Color.FromArgb(255,90,90,90);

        [Param(Name = "3nd color")]
        public Color color3 { get; set; } = Color.FromArgb(255, 220, 220, 220);

        public void Draw(Cell cell, Simulations.Environment env, WriteableBitmap targetBmp, ICollection<Point> occupiedPoints)
        {

            double dx = env.settings.DX;
            double scale = targetBmp.PixelWidth * 1.0 / (dx * env.Width);


            lock (occupiedPoints)
            {
                foreach (Point p in occupiedPoints)
                {
                    if (cell.X + cell.Y > p.X + p.Y)
                    {
                        WriteableBitmapExtensions.FillRectangle(targetBmp, (int)(scale * (p.X - dx)) - 1, (int)(scale * (p.Y - dx)) - 1, (int)(scale * (p.X + dx)), (int)(scale * (p.Y + dx)), color2);
                    }
                    else
                    {
                        WriteableBitmapExtensions.FillRectangle(targetBmp, (int)(scale * (p.X - dx)), (int)(scale * (p.Y - dx)), (int)(scale * (p.X + dx)) + 1, (int)(scale * (p.Y + dx)) + 1, color3);
                    }
                }

                foreach (Point p in occupiedPoints)
                {
                    WriteableBitmapExtensions.FillRectangle(targetBmp, (int)(scale * (p.X - dx)), (int)(scale * (p.Y - dx)), (int)(scale * (p.X + dx)), (int)(scale * (p.Y + dx)), baseColor);
                }
            }
            WriteableBitmapExtensions.FillEllipseCentered(targetBmp,(int)(scale * cell.X), (int)(scale * cell.Y), (int)(2 * scale), (int) (2 * scale), color2);
        }

        public void Initialise(Simulation sim)
        {
         
        }
    }
}
