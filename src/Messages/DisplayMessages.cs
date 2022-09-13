using ActorPipeline.Images2;
using Akka.Actor;

namespace ActorPipeline.Messages
{
    public class DisplayMessages
    {
        public sealed class AddFilterMessage
        {
            public AddFilterMessage(Props props, string filterName)
            {
                Props = props;
                FilterName = filterName;
            }

            public Props Props { get; }
            public string FilterName { get; }

            public override string ToString() =>
                $"Display_AddFilterMessage({FilterName})";
        }

        public sealed class SetImageDisplayPipelineMessage
        {
            public SetImageDisplayPipelineMessage(IActorRef pipeline)
            {
                Pipeline = pipeline;
            }

            public IActorRef Pipeline { get; }

            public override string ToString() => $"Display_SetImageDisplayPipeline({Pipeline.Path.Name})";
        }

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

            public override string ToString() => $"Display_ZoomInto({ZoomCenter}, {Delta})";
        }

        public sealed class PanByMessage
        {
            public PanByMessage(double deltaX, double deltaY)
            {
                DeltaX = deltaX;
                DeltaY = deltaY;
            }

            public double DeltaX { get; }
            public double DeltaY { get; }

            public override string ToString() => $"Display_PanBy({DeltaX}, {DeltaY})";
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

        public sealed class GetZoomedRegionMessage
        {
            public override string ToString() => "Display_GetZoomedRegionMessage";
        }

        public sealed class SaveImageMessage
        {
            public SaveImageMessage(string fileName, bool zoomedAreaOnly)
            {
                FileName = fileName;
                ZoomedAreaOnly = zoomedAreaOnly;
            }

            public string FileName { get; }
            public bool ZoomedAreaOnly { get; }

            public override string ToString() => $"Display_SaveImage({FileName}, zoomedAreaOnly: {ZoomedAreaOnly})";
        }
    }
}
