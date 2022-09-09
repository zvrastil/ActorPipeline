using ActorPipeline.Images2;

namespace ActorPipeline.Actors.Downscaling
{
    internal static class DownscalingMessages
    {
        public sealed class SetDownscalingSettingsMessage
        {
            public ImageSize InputSize { get; }
            public ImageSize DisplaySize { get; }
            public ImageFormat Format { get; }
            public FractionalImageRegion ZoomedArea { get; }

            public SetDownscalingSettingsMessage(ImageSize inputSize, ImageSize displaySize,
                ImageFormat format, FractionalImageRegion zoomedArea)
            {
                InputSize = inputSize;
                DisplaySize = displaySize;
                Format = format;
                ZoomedArea = zoomedArea;
            }

            public override string ToString() =>
                $"Downscaling_SetDownscalingSettings(inputSize: {InputSize}, displaySize: {DisplaySize}, format: {Format}, zoomedArea: {ZoomedArea})";
        }

        public sealed class SetDownscalingSizesMessage
        {
            public SetDownscalingSizesMessage(DownscalingSizes sizes)
            {
                Sizes = sizes;
            }

            public DownscalingSizes Sizes { get; }

            public override string ToString() => $"Downscaling_SetDownscalingSizes({Sizes})";
        }

        public sealed class OutputUpdatedMessage
        {
            public long OutputFormatId { get; }
            public ImageRegion Changed { get; }
            public byte[] Data { get; }

            public OutputUpdatedMessage(long outputFormatId, ImageRegion changed, byte[] data)
            {
                OutputFormatId = outputFormatId;
                Changed = changed;
                Data = data;
            }

            public override string ToString() => $"Downscaling_OutputUpdated({OutputFormatId}, {Changed}, byte[{Data.Length}])";
        }

    }
}
