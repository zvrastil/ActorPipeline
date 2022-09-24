using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace ActorPipeline.Controls
{
    public class ScanLineAdorner : Adorner
    {
        public Point Position
        {
            get => (Point)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
            nameof(Position), typeof(Point), typeof(ScanLineAdorner),
            new FrameworkPropertyMetadata(new Point(-1, -1), FrameworkPropertyMetadataOptions.AffectsRender));

        public double Length
        {
            get => (double)GetValue(LengthProperty);
            set => SetValue(LengthProperty, value);
        }

        public static readonly DependencyProperty LengthProperty = DependencyProperty.Register(
            nameof(Length), typeof(double), typeof(ScanLineAdorner),
            new FrameworkPropertyMetadata(6.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(ScanLineAdorner),
                new FrameworkPropertyMetadata(Brushes.Orange, FrameworkPropertyMetadataOptions.AffectsRender));

        public double Thickness
        {
            get => (double)GetValue(ThicknessProperty);
            set => SetValue(ThicknessProperty, value);
        }

        public static readonly DependencyProperty ThicknessProperty =
            DependencyProperty.Register(nameof(Thickness), typeof(double), typeof(ScanLineAdorner),
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public ScanLineAdorner(UIElement adornedElement) : base(adornedElement)
        {
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (Position.X >= 0 && Position.Y >= 0)
            {
                var pen = new Pen(Foreground, Thickness);
                pen.Freeze();
                
                drawingContext.DrawLine(pen, Position+new Vector(0, -1), Position+new Vector(Length, -1));
            }
        }
    }
}
