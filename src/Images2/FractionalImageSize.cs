using System;

namespace ActorPipeline.Images2
{
    public readonly struct FractionalImageSize : IEquatable<FractionalImageSize>
    {
        public static FractionalImageSize Invalid = new FractionalImageSize();

        public FractionalImageSize(double width, double height)
        {
            if (width <= 0)
            {
                throw new ArgumentException("Width must be positive.", nameof(width));
            }
            if (height <= 0)
            {
                throw new ArgumentException("Height must be positive.", nameof(width));
            }
            Width = width;
            Height = height;
        }

        public double Width { get; }
        public double Height { get; }

        public override string ToString() => $"{Width} 🞪 {Height}";

        public bool Equals(FractionalImageSize other)
        {
            // ReSharper disable CompareOfFloatsByEqualityOperator
            return Width == other.Width && Height == other.Height;
            // ReSharper restore CompareOfFloatsByEqualityOperator
        }

        public override bool Equals(object obj)
        {
            return obj is FractionalImageSize other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Width.GetHashCode() * 397) ^ Height.GetHashCode();
            }
        }

        public static bool operator ==(FractionalImageSize left, FractionalImageSize right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FractionalImageSize left, FractionalImageSize right)
        {
            return !left.Equals(right);
        }
    }
}
