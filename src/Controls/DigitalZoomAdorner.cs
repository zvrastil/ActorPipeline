using ActorPipeline.Images2;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ActorPipeline.Controls
{
    public class DigitalZoomAdorner : Adorner
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



        public double DisplayWidth
        {
            get { return (double)GetValue(DisplayWidthProperty); }
            set { SetValue(DisplayWidthProperty, value); }
        }

        public static readonly DependencyProperty DisplayWidthProperty =
            DependencyProperty.Register("DisplayWidth", typeof(double), typeof(DigitalZoomAdorner), 
                new FrameworkPropertyMetadata(default(double), FrameworkPropertyMetadataOptions.AffectsRender));


        public double DisplayHeight
        {
            get { return (double)GetValue(DisplayHeightProperty); }
            set { SetValue(DisplayHeightProperty, value); }
        }

        public static readonly DependencyProperty DisplayHeightProperty =
            DependencyProperty.Register("DisplayHeight", typeof(double), typeof(DigitalZoomAdorner),
                new FrameworkPropertyMetadata(default(double), FrameworkPropertyMetadataOptions.AffectsRender));

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
            if (ZoomedRegion != FractionalImageRegion.Invalid && adornedImage.RenderSize.Width > 0 && adornedImage.RenderSize.Height > 0 &&
                DisplayWidth > 0 && DisplayHeight > 0)
            {
                var pen = new Pen(Foreground, Thickness);
                pen.Freeze();


                var pixelsRect = ZoomedRegion
                    .ToPixels(new ImageSize((int)adornedImage.RenderSize.Width, (int)adornedImage.RenderSize.Height));
                var rect = ExpandToAspectRatio(pixelsRect, DisplayWidth/DisplayHeight);
                drawingContext.DrawRectangle(null, pen, rect);
            }
        }

        private static Rect ExpandToAspectRatio(ImageRegion region, double aspectRatio)
        {
            double currentAspectRatio = (double)region.Width / region.Height;
            double newWidth = region.Width;
            double newHeight = region.Height;
            if (aspectRatio > currentAspectRatio)
            {
                newWidth = (aspectRatio / currentAspectRatio) * region.Width;
            }
            else if (aspectRatio < currentAspectRatio)
            {
                newHeight = (1.0 / aspectRatio) * region.Height;
            }
            double newLeft = region.Center.X - newWidth / 2;
            double newTop = region.Center.Y - newHeight / 2;
            return new Rect(newLeft, newTop, newWidth, newHeight);
        }
    }
}
