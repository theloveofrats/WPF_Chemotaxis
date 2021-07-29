using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Charts;
using LiveCharts.Wpf;

namespace WPF_Chemotaxis
{
    class ChartManager
    {
        private Dictionary<Type, CartesianChart> charts = new();

        private OverlaySelector selector;
        private UniformGrid target;
        private int ticker = 0;

        private OverlaySelector.SelectionChangedEvent eve;

        public ChartManager(UniformGrid target, OverlaySelector selector)
        {
            this.target = target;
            this.selector = selector;

            this.eve = (newChartable, args) => CheckNewCharts(newChartable);
            selector.SelectionChanged += eve;
            target.Children.Clear();
        }

        public void CheckNewCharts(IGraphOnSelection newChartable)
        {
            if (selector.Selection.Count() == 0)
            {
                target.Children.Clear();
                charts.Clear();
            }
            foreach (IGraphOnSelection chartable in selector.Selection)
            {
                if (!charts.Keys.Contains(chartable.GetType()))
                {
                    CartesianChart newChart = new CartesianChart()
                    {
                        Background = new SolidColorBrush(Colors.DarkBlue),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                        VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                        Hoverable = false,
                        DataTooltip=null,
                        ToolTip = null,
                        SeriesColors = selector.ColorsCollection,
                    };
                    newChart.AxisY.Clear();
                    newChart.AxisY.Add(new Axis
                    {
                        LabelFormatter = (x) => string.Format("{0:0.00}", x),
                    });
                    // TODO set colours collection from selector, so selection and chart have same colours.
                    //newChart.SeriesColors = selector

                    charts.Add(chartable.GetType(), newChart);
                    target.Children.Add(newChart);
                }
                if (!charts[chartable.GetType()].Series.Contains(chartable.GetValues())){
                    charts[chartable.GetType()].Series.Add(chartable.GetValues());
                }
            }
        }

        public void DoChart()
        {
            if (ticker++ % 50 == 0)
            {
                foreach (Type key in charts.Keys)
                {
                    CartesianChart chart = charts[key];
                    //chart.Series.Clear();



                    foreach (IGraphOnSelection chartable in (from obj in this.selector.Selection where obj.GetType() == key select obj))
                    {

                        chartable.GetValues();
                        //if(!chart.Series.Contains) chart.Series.Add(chartable.GetValues());
                    }
                }
            }
        }
    }
}
