using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WPF_Chemotaxis.Simulations;

namespace WPF_Chemotaxis
{
    class OverlaySelector
    {
        private Simulation sim;

        private List<IGraphOnSelection> Selected = new();

        public LiveCharts.Wpf.ColorsCollection ColorsCollection { get; } = new LiveCharts.Wpf.ColorsCollection();

        public IEnumerable<IGraphOnSelection> Selection
        {
            get
            {
                return Selected;
            }
        }

        public delegate void SelectionChangedEvent(IGraphOnSelection newlySelected, EventArgs e);
        public event SelectionChangedEvent SelectionChanged;
        private void TrySelectionChanged(IGraphOnSelection newlySelected, EventArgs e)
        {
            if (SelectionChanged != null)
            {
                SelectionChanged(newlySelected, e);
            }
        }

        public OverlaySelector(Simulation sim)
        {
            this.sim = sim;
            ColorsCollection.AddRange(new Color[] { Colors.Lime, Colors.Yellow, Colors.Pink, Colors.Magenta, Colors.Cyan });
        }

        private bool drag;
        private Point dragStart;
        private Point dragEnd;

        public void OnLeftMouseDown(double x, double y)
        {
            dragStart = new Point(x, y);
            //System.Diagnostics.Debug.Print(string.Format("MouseDown at {0}:{1}", x, y));
        }

        public void OnDrag(double x, double y)
        {
            drag = true;
            dragEnd.X = x;
            dragEnd.Y = y;
        }

        public void OnLeftMouseUp(double x, double y)
        {

            bool shift = Keyboard.IsKeyDown(Key.LeftShift);
            //If it's a click, not a drag
            if (!drag)
            {
                bool clickedNothing = true;
                foreach (IGraphOnSelection cell in sim.Cells)
                {
                    if (cell.IsAtPosition(x, y))
                    {
                        clickedNothing = false; 
                        if (Selected.Contains(cell)) continue;
                        if (!shift) Selected.Clear();
                        Selected.Add(cell);
                        TrySelectionChanged(cell, new EventArgs());
                        break;
                    }
                }
                if (clickedNothing) //First see if it's a click on an environment region. If not, clear selection.
                {
                    if (!shift) Selected.Clear();
                    TrySelectionChanged(null, new EventArgs());
                }
            }
            else
            {
                if (!shift) Selected.Clear();
                double xMin = Math.Min(dragStart.X, dragEnd.X);
                double xMax = Math.Max(dragStart.X, dragEnd.X);
                double yMin = Math.Min(dragStart.Y, dragEnd.Y);
                double yMax = Math.Max(dragStart.Y, dragEnd.Y);

                IGraphOnSelection sampler = new AreaSampler(this.sim, xMin, yMin, xMax - xMin, yMax - yMin);
                Selected.Add(sampler);
                TrySelectionChanged(sampler, new EventArgs());

                drag = false;
            }

            //System.Diagnostics.Debug.Print(string.Format("Mouse Up at {0}:{1}, drag={2}, selected={3}", x, y, drag, (Selected.Count>0)));
        }

        public void DrawSelection(Simulation sim, WriteableBitmap target)
        {
            int i = 0;
            Color clr;
            foreach(IGraphOnSelection selected in this.Selection)
            {
                clr = ColorsCollection[i % ColorsCollection.Count];
                selected.DrawHighlight(sim, target, clr);
                i++;
            }
        }
    }
}
