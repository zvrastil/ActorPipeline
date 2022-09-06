using ActorPipeline.Images2;

namespace ActorPipeline.Crates
{
    internal readonly struct ImageFormatCrate
    {
        public ImageFormatCrate(ImageFormat format, string description)
        {
            Format = format;
            Description = description;
        }

        public ImageFormat Format { get; }
        public string Description { get; }
    }
}
