using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WPF_Chemotaxis.Simulations;

namespace WPF_Chemotaxis.Model
{
    /// <summary>
    /// Base interface for drawing cells in the simulation overlay. Any concrete ICellDrawHandler is automatically included in a dropdown list for use.
    /// </summary>
    public interface ICellDrawHandler
    {
        /// <summary>
        /// Called when the overlay is redrawn.
        /// </summary>
        /// <param name="c">The cell being drawn</param>
        /// <param name="env">The environment the cell is in</param>
        /// <param name="bmp">The target bitmap that is being drawn on (this is already open to drawing).</param>
        /// <param name="points">The list of points the cell currently occupies</param>
        public void Draw(Cell c, Simulations.Environment env, WriteableBitmap bmp, ICollection<Point> points);
        /// <summary>
        /// Called when Simulation sim starts, in case any initialisation is required.
        /// </summary>
        public void Initialise(Simulation sim);
    }
}
