using System;

namespace ActorPipeline.Images2
{
    public readonly struct FractionalImagePosition : IEquatable<FractionalImagePosition>
    {
        public static FractionalImagePosition Invalid = new FractionalImagePosition(false);

        private FractionalImagePosition(bool valid)
        {
            X = -1;
            Y = -1;
        }

        public FractionalImagePosition(double x, double y)
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

        public double X { get; }
        public double Y { get; }

        public override string ToString() => $"[{X}, {Y}]";
        public bool Equals(FractionalImagePosition other)
        {
            // ReSharper disable CompareOfFloatsByEqualityOperator
            return X == other.X && Y == other.Y;
            // ReSharper restore CompareOfFloatsByEqualityOperator
        }

        public override bool Equals(object obj)
        {
            return obj is FractionalImagePosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public static bool operator ==(FractionalImagePosition left, FractionalImagePosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FractionalImagePosition left, FractionalImagePosition right)
        {
            return !left.Equals(right);
        }
    }
}
