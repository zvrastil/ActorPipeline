using ActorPipeline.Images2;

namespace ActorPipeline.Messages
{
    public static class DisplayMessages
    {
        public sealed class SetDisplaySizeMessage
        {
            public ImageSize DisplaySize { get; }

            public SetDisplaySizeMessage(ImageSize displaySize)
            {
                DisplaySize = displaySize;
            }
        }

        public sealed class SetDigitalZoomMessage
        {
            public double ZoomFactor { get; }
            public double PanX { get; }
            public double PanY { get; }

            public SetDigitalZoomMessage(double zoomFactor, double panX, double panY)
            {
                ZoomFactor = zoomFactor;
                PanX = panX;
                PanY = panY;
            }
        }

        public sealed class SetDisplaySettingsMessage
        {
            public FractionalImageRegion RelativeRegion { get; }
            public ImageSize DisplaySize { get; }

            public SetDisplaySettingsMessage(FractionalImageRegion relativeRegion, ImageSize displaySize)
            {
                RelativeRegion = relativeRegion;
                DisplaySize = displaySize;
            }

        }
    }
}
