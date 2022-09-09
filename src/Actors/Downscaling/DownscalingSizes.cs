using ActorPipeline.Images2;

namespace ActorPipeline.Actors.Downscaling
{
    public sealed class DownscalingSizes
    {
        public long Id { get; }
        public ImageSize InputSize { get; }
        public FractionalImageRegion FinalInputRegion { get; }
        public ImageSize FinalOutputSize { get; }
        public ImageFormat Format { get; }

        public DownscalingSizes(long id, ImageSize inputSize, FractionalImageRegion finalInputRegion, ImageSize finalOutputSize, ImageFormat format)
        {
            Id = id;
            InputSize = inputSize;
            FinalInputRegion = finalInputRegion;
            FinalOutputSize = finalOutputSize;
            Format = format;
        }

        public override string ToString() =>
            $"[id: {Id}, inputSize: {InputSize}, finalInputRegion: {FinalInputRegion}, finalOutputSize:{FinalOutputSize}, format: {Format}]";
    }
}
