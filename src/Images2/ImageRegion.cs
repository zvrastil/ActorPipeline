using System;

namespace ActorPipeline.Images2
{
    public readonly struct ImageRegion : IEquatable<ImageRegion>
    {
        public static ImageRegion Invalid = new ImageRegion();
        public static ImageRegion WholeImage(IImage image) => new ImageRegion(0, 0, image.Width, image.Height);

        public ImageRegion(int left, int top, int width, int height)
        {
            if (left < 0)
            {
                throw new ArgumentException("Left must be non-negative", new ArgumentException(nameof(left)));
            }
            if (top < 0)
            {
                throw new ArgumentException("Left must be non-negative", new ArgumentException(nameof(top)));
            }
            if (width <= 0)
            {
                throw new ArgumentException("Width must be positive", new ArgumentException(nameof(width)));
            }
            if (height <= 0)
            {
                throw new ArgumentException("Height must be positive", new ArgumentException(nameof(height)));
            }

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

        public ImageSize Size => new ImageSize(Width, Height);

        public override string ToString() => $"[{Left}, {Top}, {Right}, {Bottom}] {Width}x{Height}";

        public bool Equals(ImageRegion other)
        {
            return Left == other.Left && Top == other.Top && Width == other.Width && Height == other.Height;
        }

        public override bool Equals(object obj)
        {
            return obj is ImageRegion other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Left;
                hashCode = (hashCode * 397) ^ Top;
                hashCode = (hashCode * 397) ^ Width;
                hashCode = (hashCode * 397) ^ Height;
                return hashCode;
            }
        }

        public static bool operator ==(ImageRegion left, ImageRegion right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ImageRegion left, ImageRegion right)
        {
            return !left.Equals(right);
        }
    }
}
