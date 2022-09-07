using ActorPipeline.Images2.Images;

namespace ActorPipeline.Images2.Factories
{
    public class InMemoryImageFactory : IImageFactory
    {
        public static IImageFactory Instance { get; } = new InMemoryImageFactory();
        public IImage Create(ImageSize size, ImageFormat format) => new InMemoryImage(size, format);
    }
}
