using ActorPipeline.Images2;
using Akka.Actor;

namespace ActorPipeline.Actors.Downscaling
{
    internal static class DownscalingMessages
    {
        public sealed class SetDownscalingSettingsMessage
        {
            public IActorRef ForActor { get; }
            public ImageSize InputSize { get; }
            public ImageSize DisplaySize { get; }
            public ImageFormat Format { get; }
            public FractionalImageRegion ZoomedArea { get; }

            public SetDownscalingSettingsMessage(IActorRef forActor, ImageSize inputSize, ImageSize displaySize,
                ImageFormat format, FractionalImageRegion zoomedArea)
            {
                ForActor = forActor;
                InputSize = inputSize;
                DisplaySize = displaySize;
                Format = format;
                ZoomedArea = zoomedArea;
            }

            public override string ToString() =>
                $"Downscaling_SetDownscalingSettings(for: {ForActor.Path.Name}, inputSize: {InputSize}, displaySize: {DisplaySize}, format: {Format}, zoomedArea: {ZoomedArea})";
        }

        public sealed class SetDownscalingSizesMessage
        {
            public SetDownscalingSizesMessage(IActorRef forActor, DownscalingSizes sizes)
            {
                ForActor = forActor;
                Sizes = sizes;
            }

            public IActorRef ForActor { get; }
            public DownscalingSizes Sizes { get; }

            public override string ToString() => $"Downscaling_SetDownscalingSizes({ForActor.Path.Name}, {Sizes})";
        }

        public sealed class OutputUpdatedMessage
        {
            public IActorRef ForActor { get; }
            public long OutputFormatId { get; }
            public ImageRegion Changed { get; }
            public byte[] Data { get; }

            public OutputUpdatedMessage(IActorRef forActor, long outputFormatId, ImageRegion changed, byte[] data)
            {
                ForActor = forActor;
                OutputFormatId = outputFormatId;
                Changed = changed;
                Data = data;                
            }

            public override string ToString() => $"Downscaling_OutputUpdated({OutputFormatId}, {Changed}, byte[{Data.Length}])";
        }

        public sealed class PartialResizeMessage
        {
            public ImageRegion UpdatedRegion { get; }

            public PartialResizeMessage(ImageRegion updatedRegion)
            {
                UpdatedRegion = updatedRegion;
            }

            public override string ToString() => $"Downscaling_PartialResize({UpdatedRegion})";
        }

        public sealed class ResizeCompletedMessage
        {
            public override string ToString() => "Downscaling_ResizeCompleted";
        }

    }
}
