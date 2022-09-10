using ActorPipeline.Images2;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ActorPipeline.Controls
{
    internal class DigitalZoomAdorner : Adorner
    {
        private readonly Image adornedImage;

        public FractionalImageRegion ZoomedRegion
        {
            get { return (FractionalImageRegion)GetValue(ZoomedRegionProperty); }
            set { SetValue(ZoomedRegionProperty, value); }
        }

        public static readonly DependencyProperty ZoomedRegionProperty =
            DependencyProperty.Register("ZoomedRegion", typeof(FractionalImageRegion), typeof(DigitalZoomAdorner), 
                new FrameworkPropertyMetadata(FractionalImageRegion.Invalid, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush Foreground
        {
            get { return (Brush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register("Foreground", typeof(Brush), typeof(DigitalZoomAdorner), 
                new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

        public double Thickness
        {
            get { return (double)GetValue(ThicknessProperty); }
            set { SetValue(ThicknessProperty, value); }
        }

        public static readonly DependencyProperty ThicknessProperty =
            DependencyProperty.Register("Thickness", typeof(double), typeof(DigitalZoomAdorner), 
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public DigitalZoomAdorner(Image adornedElement) : base(adornedElement)
        {
            adornedImage = adornedElement;
            IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (ZoomedRegion != FractionalImageRegion.Invalid && adornedImage.RenderSize.Width > 0 && adornedImage.RenderSize.Height > 0)
            {
                var pen = new Pen(Foreground, Thickness);
                pen.Freeze();

                var pixelsRect = ZoomedRegion.ToFractionalPixels(new ImageSize((int)adornedImage.RenderSize.Width, (int)adornedImage.RenderSize.Height));
                var rect = new Rect(pixelsRect.Left, pixelsRect.Top, pixelsRect.Width, pixelsRect.Height);
                drawingContext.DrawRectangle(null, pen, rect);
            }
        }
    }
}
