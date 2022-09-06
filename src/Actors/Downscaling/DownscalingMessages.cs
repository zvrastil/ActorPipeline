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

    }
}
