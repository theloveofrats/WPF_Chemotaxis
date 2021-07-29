using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Chemotaxis
{
    public class CenteredDoubleRange
    {

        private static SciRand rnd = new SciRand();
        public double Value { get; set; }
        public double Range { get; set; }

        public double? HardMin { get; set; } = null;

        public double? HardMax { get; set; } = null;

        public CenteredDoubleRange(double centre, double range)
        {
            Value = centre;
            Range = range;
        }

        bool Contains(double compare)
        {
            return Value - Range <= compare && Value + Range >= compare;
        }
        bool Contains(CenteredDoubleRange range)
        {
            return Value - Range <= range.Value-range.Range  && Value + Range >= range.Value+range.Range;
        }

        public double RandomInRange
        {
            get
            {
                double min = Value - Range;
                double max = Value + Range;
                if (HardMin != null && HardMin > min) min = HardMin.Value;
                if (HardMax != null && HardMax < max) max = HardMax.Value;

                return rnd.NextDouble(min, max);
            }
        }
    }
}
