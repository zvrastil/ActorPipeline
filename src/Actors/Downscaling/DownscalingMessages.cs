using ActorPipeline.Images2;

namespace ActorPipeline.Actors.Downscaling
{
    internal static class DownscalingMessages
    {
        public sealed class SetDownscalingSettingsMessage
        {
            public int InputWidth { get; }
            public int InputHeight { get; }
            public int DisplayWidth { get; }
            public int DisplayHeight { get; }
            public ImageFormat Format { get; }
            public FractionalImageRegion ZoomedArea { get; }

            public SetDownscalingSettingsMessage(int inputWidth, int inputHeight, int displayWidth, int displayHeight,
                ImageFormat format, FractionalImageRegion zoomedArea)
            {
                InputWidth = inputWidth;
                InputHeight = inputHeight;
                DisplayWidth = displayWidth;
                DisplayHeight = displayHeight;
                Format = format;
                ZoomedArea = zoomedArea;
            }
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
        }

    }
}
