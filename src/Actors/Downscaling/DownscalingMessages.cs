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
            public ImageSize InputSize { get; }
            public ImageRegion FinalInputRegion { get; }
            public ImageSize FinalOutputSize { get; }
            public ImageFormat Format { get; }

            public SetDownscalingSizesMessage(ImageSize inputSize, ImageRegion finalInputRegion, ImageSize finalOutputSize, ImageFormat format)
            {
                InputSize = inputSize;
                FinalInputRegion = finalInputRegion;
                FinalOutputSize = finalOutputSize;
                Format = format;
            }

            public override string ToString() =>
                $"Downscaling_SetDownscalingSizes(inputSize: {InputSize}, finalInputRegion: {FinalInputRegion}, finalOutputSize:{FinalOutputSize}, format: {Format})";
        }

        public sealed class OutputUpdatedMessage
        {
            public ImageRegion Changed { get; }
            public byte[] Data { get; }

            public OutputUpdatedMessage(ImageRegion changed, byte[] data)
            {
                Changed = changed;
                Data = data;
            }

            public override string ToString() => $"Downscaling_OutputUpdated({Changed}, byte[{Data.Length}])";
        }

    }
}
