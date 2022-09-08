using System;

namespace ActorPipeline.Images2
{
    public readonly struct ImagePosition : IEquatable<ImagePosition>
    {
        public static ImagePosition Invalid = new ImagePosition(false);

        private ImagePosition(bool valid)
        {
            X = -1;
            Y = -1;
        }

        public ImagePosition(int x, int y)
        {
            if (x < 0)
            {
                throw new ArgumentException("X must be positive.", nameof(x));
            }
            if (y < 0)
            {
                throw new ArgumentException("Y must be positive.", nameof(y));
            }
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public override string ToString() => $"[{X}, {Y}]";
        public bool Equals(ImagePosition other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is ImagePosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public static bool operator ==(ImagePosition left, ImagePosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ImagePosition left, ImagePosition right)
        {
            return !left.Equals(right);
        }
    }
}
