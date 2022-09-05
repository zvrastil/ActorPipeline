using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActorPipeline.Images
{
    public struct IntRectangle : IEquatable<IntRectangle>
    {
        public static IntRectangle Empty() => new IntRectangle(int.MaxValue, int.MaxValue, int.MinValue, int.MinValue);

        public IntRectangle(int left, int top, int width, int height)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }

        public int Left { get; }
        public int Top { get; }
        public int Width { get; }
        public int Height { get; }

        public int Right => Left + Width;
        public int Bottom => Top + Height;

        public IntRectangle Union(IntRectangle other)
        {
            int left = Math.Min(Left, other.Left);
            int top = Math.Min(Top, other.Top);
            int right = Math.Max(Right, other.Right);
            int bottom = Math.Max(Bottom, other.Bottom);

            return new IntRectangle(left, top, right - left, bottom - top);
        }

        public override string ToString() => $"({Left}, {Top}, {Width}x{Height})";

        public bool Equals(IntRectangle other)
        {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            if (obj is IntRectangle ir)
            {
                return Equals(ir);
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Left;
                hash = hash * 31 + Top;
                hash = hash * 31 + Width;
                hash = hash * 31 + Height;
                return hash;
            }
        }

        public static bool operator ==(IntRectangle a, IntRectangle b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(IntRectangle a, IntRectangle b)
        {
            return !(a == b);
        }
    }
}
