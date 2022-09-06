using System;

namespace ActorPipeline.Images2
{
    public readonly struct ImageSize : IEquatable<ImageSize>
    {
        public static ImageSize Invalid = new ImageSize();

        public ImageSize(int width, int height)
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

        public int Width { get; }
        public int Height { get; }

        public override string ToString() => $"{Width}x{Height}";
        public bool Equals(ImageSize other)
        {
            return Width == other.Width && Height == other.Height;
        }

        public override bool Equals(object obj)
        {
            return obj is ImageSize other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Width * 397) ^ Height;
            }
        }

        public static bool operator ==(ImageSize left, ImageSize right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ImageSize left, ImageSize right)
        {
            return !left.Equals(right);
        }
    }
}
