using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Chemotaxis
{
    public class CenteredIntRange
    {
        private static SciRand rnd = new SciRand();

        public int Value { get; set; }
        public int Range { get; set; }

        public int? HardMin { get; set; } = null;
        public int? HardMax { get; set; } = null;

        public CenteredIntRange(int centre, int range)
        {
            Value = centre;
            Range = range;
        }

        bool Contains(int compare)
        {
            return Value - Range <= compare && Value + Range >= compare;
        }
        bool Contains(CenteredIntRange range)
        {
            return Value - Range <= range.Value-range.Range  && Value + Range >= range.Value+range.Range;
        }


        public int RandomInRange
        {
            get
            {
                int min = Value - Range;
                int max = Value + Range;
                if (HardMin != null && HardMin > min) min = HardMin.Value;
                if (HardMax != null && HardMax < max) max = HardMax.Value;

                return rnd.NextInteger(min, max);
            }
        }
    }
}
