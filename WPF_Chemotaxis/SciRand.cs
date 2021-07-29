using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Chemotaxis
{
    public class SciRand
    {
        private System.Random rnd;
        private bool spareGaussianCached = false;
        private double spareGaussian;
        private static List<char> alphabet_upper = new List<char>(new char[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' });
        private static List<char> alphabet_lower = new List<char>(new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' });

        public SciRand()
        {
            rnd = new System.Random();
        }

        public SciRand(string seed)
        {
            rnd = new System.Random(seed.GetHashCode());
        }

        public SciRand(int seed)
        {
            rnd = new System.Random(seed);
        }

        public void Reseed(string seed)
        {
            rnd = new System.Random(seed.GetHashCode());
            spareGaussian = 0;
            spareGaussianCached = false;
        }

        public T RandomElement<T>(List<T> list)
        {
            return list[rnd.Next(0, list.Count)];
        }

        public T RandomElementWeighted<T>(List<T> list, Func<T, double> weight)
        {
            double total = 0;
            foreach (T item in list)
            {
                total += weight(item);
            }
            double choiceWeight = NextDouble(0, total);
            total = 0;
            foreach (T item in list)
            {
                total += weight(item);
                if (total >= choiceWeight) return item;
            }
            return list[0];
        }

        public void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count; i > 0; i--)
            {
                Swap(list, i - 1, rnd.Next(0, i));
            }
        }

        private void Swap<T>(List<T> list, int i, int j)
        {
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }

        public List<T> RandomElementsWeighted<T>(List<T> list, int numSelected, Func<T, double> weight)
        {
            List<T> workingList = new List<T>();
            workingList.AddRange(list);
            if (numSelected >= list.Count) return workingList;

            double total = 0;
            double running;
            foreach (T item in workingList)
            {
                total += weight(item);
            }

            List<T> listOut = new List<T>();
            T selected = workingList[0];
            double choiceWeight;
            for (int n = 0; n < numSelected; n++)
            {
                choiceWeight = NextDouble(0, total);
                running = 0;

                for (int i = 0; i < workingList.Count; i++)
                {
                    T item = list[i];
                    running += weight(item);
                    if (running >= choiceWeight)
                    {
                        selected = item;
                    }
                }
                total -= weight(selected);
                listOut.Add(selected);
                workingList.Remove(selected);
            }

            return listOut;
        }

        public double NextDouble(double min, double max)
        {
            return min + value * (max - min);
        }

        public double NextDouble()
        {
            return value;
        }

        public double value
        {
            get
            {
                return rnd.NextDouble();
            }
        }

        // Using the Marsaglia method.
        public double NextGaussian(double m, double s)
        {
            if (spareGaussianCached)
            {
                spareGaussianCached = false;
                return (s * spareGaussian + m);
            }
            else
            {
                double u, v, w;
                
                lock (rnd)
                {
                    do
                    {
                        u = rnd.NextDouble() * 2d - 1d;
                        v = rnd.NextDouble() * 2d - 1d;
                        w = u * u + v * v;
                        
                    } while (w >= 1 || w == 0);
                }
                double mlt = Math.Sqrt(-2d * Math.Log(w) / w);

                spareGaussian = v * mlt;
                spareGaussianCached = true;
                return m + s * u * mlt;
            }
        }

        public int NextInteger(int min, int max)
        {
            return rnd.Next(min, max);
        }

        public double NextGamma(double a, double b)
        {

            double d = a - (1d / 3d);
            if (a < 1) d += 1;
            double c = 1 / Math.Sqrt(9d * d);

            double x, v, lnu, val;
            x = v = lnu = val = 0;

            do
            {
                x = NextGaussian(0, 1);
                v = (1d + c * x) * (1d + c * x) * (1d + c * x);

                if (v > 0)
                {
                    lnu = Math.Log(rnd.NextDouble());
                    if (lnu < 0.5d * x * x + d * (1 + Math.Log(v) - v))
                    {
                        val = (d * v) * b;
                        break;
                    }
                }
            } while (lnu < 0.5d * x * x + d * (1 + Math.Log(v) - v) || val == 0);
            if (a < 1) val *= Math.Pow(rnd.NextDouble(), (1d / a));

            return val;

        }

        public double NextLogNormal(double m, double s)
        {
            return Math.Exp(NextGaussian(m, s));
        }

        public double NextBeta(double a, double b)
        {

            if (a == b && b == 1d) return rnd.NextDouble();

            double x = NextGamma(a, 1d);
            double y = NextGamma(b, 1d);

            return (x / (x + y));
        }

        public char NextChar(bool upper)
        {
            if (upper) return RandomElement<char>(alphabet_upper);
            else return RandomElement<char>(alphabet_lower);
        }

        public string RandomCharString(bool upper, int length)
        {
            string strand = "";
            for (int i = 0; i < length; i++)
            {
                strand += NextChar(upper);
            }
            return strand;
        }

        public string RandomNumString(int length)
        {
            if (length <= 0) return "";
            string strand = NextInteger(1, 9).ToString();
            for (int i = 0; i < length - 1; i++)
            {
                strand += NextInteger(0, 9).ToString();
            }
            return strand;
        }

        public bool NextBoolean()
        {
            return (rnd.NextDouble() < 0.5);
        }
    }
}
