using ActorPipeline.Images;

namespace ActorPipeline.Crates
{
    internal struct ImageFormatCrate
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
