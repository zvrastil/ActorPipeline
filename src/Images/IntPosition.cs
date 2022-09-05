using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActorPipeline.Images
{
    internal struct IntPosition : IEquatable<IntPosition>
    {
        public IntPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public override string ToString() => $"[{X}, {Y}]";        

        public bool Equals(IntPosition other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            if (obj is IntPosition ip)
            {
                return Equals(ip);
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + X;
                hash = hash * 31 + Y;
                return hash;
            }
        }

        public static bool operator ==(IntPosition a, IntPosition b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(IntPosition a, IntPosition b)
        {
            return !(a == b);
        }
    }
}
