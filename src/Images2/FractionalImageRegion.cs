using System;
using System.Windows;

namespace ActorPipeline.Images2
{
    public readonly struct FractionalImageRegion : IEquatable<FractionalImageRegion>
    {
        public static FractionalImageRegion Invalid = new FractionalImageRegion();
        public static FractionalImageRegion WholeImage() => new FractionalImageRegion(0, 0, 1, 1);

        public FractionalImageRegion(double left, double top, double width, double height)
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

        public double Left { get; }
        public double Top { get; }
        public double Width { get; }
        public double Height { get; }

        public double Right => Left + Width;
        public double Bottom => Top + Height;

        public FractionalImageSize Size => new FractionalImageSize(Width, Height);

        public FractionalImagePosition Center => new FractionalImagePosition(Left + Width / 2.0, Top + Height / 2.0);

        public FractionalImageRegion ZoomIn(double factor)
        {
            double centerX = Left + Width / 2.0;
            double centerY = Top + Height / 2.0;
            double width = Width / factor;
            double height = Height / factor;
            return new FractionalImageRegion(centerX - width / 2.0, centerY - height / 2.0, width, height);
        }

        public FractionalImageRegion PanSafe(double panX, double panY)
        {
            double newLeft = Fit(panX - Width / 2.0, 0, 1.0 - Width);
            double newTop = Fit(panY - Height / 2.0, 0, 1.0 - Height);
            return new FractionalImageRegion(newLeft, newTop, Width, Height);
        }

        private static double Fit(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public FractionalImageRegion ToFractionalPixels(ImageSize sourceSize)
        {
            return new FractionalImageRegion(
                Left * sourceSize.Width,
                Top * sourceSize.Height,
                Width * sourceSize.Width,
                Height * sourceSize.Height
            );
        }

        public ImageRegion ToPixels(ImageSize sourceSize)
        {
            return new ImageRegion(
                (int)(Left * sourceSize.Width),
                (int)(Top * sourceSize.Height),
                (int)(Width * sourceSize.Width),
                (int)(Height * sourceSize.Height)
                );
        }

        public FractionalImageRegion IntersectWith(ImageRegion reducedArea)
        {
            double left = Math.Max(Left, reducedArea.Left);
            double top = Math.Max(Top, reducedArea.Top);
            double right = Math.Min(Right, reducedArea.Right);
            double bottom = Math.Min(Bottom, reducedArea.Bottom);
            double width = Math.Max(0, right - left);
            double height = Math.Max(0, bottom - top);
            if (width > 0 && height > 0)
            {
                return new FractionalImageRegion(left, top, width, height);
            }

            return Invalid;
        }

        public FractionalImageRegion ExpandSafelyTo(ImageSize idealSize, ImageSize fullImageSize)
        {
            double newWidth = Math.Min(idealSize.Width, fullImageSize.Width);
            double newHeight = Math.Min(idealSize.Height, fullImageSize.Height);
            double newLeft = Math.Max(0, Center.X - newWidth / 2);
            double newTop = Math.Max(0, Center.Y - newHeight / 2);
            return new FractionalImageRegion(newLeft, newTop, newWidth, newHeight);
        }

        public override string ToString() => $"[{Left}, {Top}, {Right}, {Bottom}] {Width}x{Height}";

        public bool Equals(FractionalImageRegion other)
        {
            return Left.Equals(other.Left) && Top.Equals(other.Top) && Width.Equals(other.Width) && Height.Equals(other.Height);
        }

        public override bool Equals(object obj)
        {
            return obj is FractionalImageRegion other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Left.GetHashCode();
                hashCode = (hashCode * 397) ^ Top.GetHashCode();
                hashCode = (hashCode * 397) ^ Width.GetHashCode();
                hashCode = (hashCode * 397) ^ Height.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(FractionalImageRegion left, FractionalImageRegion right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FractionalImageRegion left, FractionalImageRegion right)
        {
            return !left.Equals(right);
        }
    }
}
