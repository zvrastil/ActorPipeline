using ActorPipeline.Images2;

namespace ActorPipeline.Actors.Downscaling
{
    internal static class DownscalingMessages
    {
        public sealed class SetDownscalingSettingsMessage
        {
            public int InputWidth { get; }
            public int InputHeight { get; }
            public int OutputWidth { get; }
            public int OutputHeight { get; }
            public ImageFormat Format { get; }
            public FractionalImageRegion ZoomedArea { get; }

            public SetDownscalingSettingsMessage(int inputWidth, int inputHeight, int outputWidth, int outputHeight,
                ImageFormat format, FractionalImageRegion zoomedArea)
            {
                InputWidth = inputWidth;
                InputHeight = inputHeight;
                OutputWidth = outputWidth;
                OutputHeight = outputHeight;
                Format = format;
                ZoomedArea = zoomedArea;
            }
        }

    }
}
