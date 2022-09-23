using ActorPipeline.Images2;
using ActorPipeline.Mailbox;
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

        public sealed class SetDigitalZoomMessage : IReplaceableMessage
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
            public bool CanReplace(IReplaceableMessage other) => other is SetDigitalZoomMessage;
        }

        public sealed class ZoomIntoMessage : IReplaceableMessage
        {
            public FractionalImagePosition ZoomCenter { get; }
            public double Delta { get; }

            public ZoomIntoMessage(FractionalImagePosition zoomCenter, double delta)
            {
                ZoomCenter = zoomCenter;
                Delta = delta;
            }

            public override string ToString() => $"Display_ZoomInto({ZoomCenter}, {Delta})";
            public bool CanReplace(IReplaceableMessage other) => other is ZoomIntoMessage;
        }

        public sealed class PanByMessage : IReplaceableMessage
        {
            public PanByMessage(double deltaX, double deltaY)
            {
                DeltaX = deltaX;
                DeltaY = deltaY;
            }

            public double DeltaX { get; }
            public double DeltaY { get; }

            public override string ToString() => $"Display_PanBy({DeltaX}, {DeltaY})";
            public bool CanReplace(IReplaceableMessage other) => other is PanByMessage;
        }

        public sealed class SetDisplaySettingsMessage : IReplaceableMessage
        {
            public FractionalImageRegion RelativeRegion { get; }
            public ImageSize DisplaySize { get; }

            public SetDisplaySettingsMessage(FractionalImageRegion relativeRegion, ImageSize displaySize)
            {
                RelativeRegion = relativeRegion;
                DisplaySize = displaySize;
            }

            public override string ToString() => $"Display_SetDisplaySettings({RelativeRegion}, {DisplaySize})";
            public bool CanReplace(IReplaceableMessage other) => other is SetDisplaySettingsMessage;
        }

        public sealed class GetZoomedRegionMessage
        {
            public override string ToString() => "Display_GetZoomedRegionMessage";
        }

        public sealed class SaveImageMessage
        {
            public SaveImageMessage(long requestId, string fileName, bool zoomedAreaOnly)
            {
                RequestId = requestId;
                FileName = fileName;
                ZoomedAreaOnly = zoomedAreaOnly;
            }

            public long RequestId { get; }
            public string FileName { get; }
            public bool ZoomedAreaOnly { get; }

            public override string ToString() => $"Display_SaveImage({RequestId}, {FileName}, zoomedAreaOnly: {ZoomedAreaOnly})";
        }

        public sealed class ImageSavedMessage
        {
            public ImageSavedMessage(long requestId)
            {
                RequestId = requestId;
            }

            public long RequestId { get; }

            public override string ToString() => $"Display_ImageSaved({RequestId})";
        }
    }
}
