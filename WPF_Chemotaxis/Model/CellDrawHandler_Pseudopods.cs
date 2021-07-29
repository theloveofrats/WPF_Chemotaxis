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
    /// A cell drawing class that draws two pseudopods in, a large one for the current cell direction and a small one for the
    /// longer-term average.
    /// </summary>
    class CellDrawHandler_Pseudopods : LabelledLinkable, ICellDrawHandler
    {

        [Param(Name = "Base Color")]
        public Color baseColor = Colors.DarkCyan;

        [Param(Name = "2nd color")]
        public Color color2 = Colors.Black;

        [Param(Name = "3nd color")]
        public Color color3 = Colors.White;

        private Dictionary<Cell,Vector> previousMovement = new();
        private Dictionary<Cell,Vector> recentMovement = new();

        public void Draw(Cell cell, Simulations.Environment env, WriteableBitmap targetBmp, ICollection<Point> occupiedPoints)
        {
            double dx = env.settings.DX;
            double scale = targetBmp.PixelWidth * 1.0 / (dx * env.Width);
            Vector newDir = new Vector(cell.vx, cell.vy);
            newDir.Normalize();

            if (!previousMovement.ContainsKey(cell)) previousMovement.Add(cell, new Vector());
            if (!recentMovement.ContainsKey(cell)) recentMovement.Add(cell, new Vector());

            recentMovement[cell] = 0.9 * recentMovement[cell] + 0.1 * newDir;
            recentMovement[cell].Normalize();

            previousMovement[cell] = 0.99 * previousMovement[cell] + 0.01 * newDir;
            previousMovement[cell].Normalize();

            double r0 = 0.4*cell.radius*scale;
            double r1 = r0 * 2.4;
            double r2 = r0 *3;

            int x = (int)(cell.X*scale);
            int y = (int)(cell.Y*scale);

            int[] points1 = GetCurvePoints(x, y, previousMovement[cell], r1, r0);
            int[] points2 = GetCurvePoints(x, y, recentMovement[cell], r2, r0);

            WriteableBitmapExtensions.FillCurveClosed(targetBmp, points1, 0.75f, baseColor);
            WriteableBitmapExtensions.DrawCurveClosed(targetBmp, points1, 0.75f, color2);
            WriteableBitmapExtensions.FillCurveClosed(targetBmp, points2, 0.75f, baseColor);
            WriteableBitmapExtensions.DrawCurveClosed(targetBmp, points2, 0.75f, color2);
            
        }

        public void Initialise(Simulation sim)
        {
          
        }

        private int[] GetCurvePoints(int x0, int y0, Vector direction, double length, double r)
        {
            int[] points = new int[16];
            Vector orth = new Vector(direction.Y, -direction.X);
            orth.Normalize();

            points[0] = (int)Math.Round(x0 - r * orth.X);
            points[1] = (int)Math.Round(y0 - r * orth.Y);

            points[2] = (int)Math.Round(x0 - r * direction.X);
            points[3] = (int)Math.Round(y0 - r * direction.Y);

            points[4] = (int)Math.Round(x0 + r * orth.X);
            points[5] = (int)Math.Round(y0 + r * orth.Y);


            points[6] = (int)Math.Round(x0 + direction.X * 0.5*length + r * orth.X);
            points[7] = (int)Math.Round(y0 + direction.Y * 0.5*length + r * orth.Y);


            points[8] = (int)Math.Round(x0 + direction.X * length + r * orth.X);
            points[9] = (int)Math.Round(y0 + direction.Y * length + r * orth.Y);

            points[10] = (int)Math.Round(x0 + direction.X * length + r * direction.X);
            points[11] = (int)Math.Round(y0 + direction.Y * length + r * direction.Y);

            points[12] = (int)Math.Round(x0 + direction.X * length - r * orth.X);
            points[13] = (int)Math.Round(y0 + direction.Y * length - r * orth.Y);

            points[14] = (int)Math.Round(x0 + direction.X * 0.5 * length - r * orth.X);
            points[15] = (int)Math.Round(y0 + direction.Y * 0.5 * length - r * orth.Y);

            return points;
        }
    }
}
