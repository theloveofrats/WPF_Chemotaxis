using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Chemotaxis
{
    public struct Vector2Int
    {
        private int x;
        private int y;
        public int X
        {
            get
            {
                return x;
            }
        }
        public int Y
        {
            get
            {
                return y;
            }
        }

        public Vector2Int(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override bool Equals(object obj)
        {
            if(!(obj is Vector2Int))
            {
                return false;
            }
            Vector2Int other = (Vector2Int)obj;

            return (other.X == this.X && other.Y == this.Y);
        }

        public override int GetHashCode()
        {
            int hash = 41;
            hash = (hash * 7)  + x.GetHashCode();
            hash = (hash * 13) * y.GetHashCode();
            return hash;
        }
    }
}
