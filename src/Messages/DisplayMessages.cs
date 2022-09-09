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

            public override string ToString() => $"Display_SetDisplaySize({DisplaySize})";
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

            public override string ToString() => $"Display_SetDigitalZoom({ZoomFactor}, {PanX}, {PanY})";
        }

        public sealed class ZoomIntoMessage
        {
            public FractionalImagePosition ZoomCenter { get; }
            public double Delta { get; }

            public ZoomIntoMessage(FractionalImagePosition zoomCenter, double delta)
            {
                ZoomCenter = zoomCenter;
                Delta = delta;
            }

            public override string ToString() => $"Display_ZoomIntoMessage({ZoomCenter}, {Delta})";
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

            public override string ToString() => $"Display_SetDisplaySettings({RelativeRegion}, {DisplaySize})";

        }
    }
}
