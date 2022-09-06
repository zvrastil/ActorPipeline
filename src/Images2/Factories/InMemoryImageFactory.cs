using ActorPipeline.Images2.Images;

namespace ActorPipeline.Images2.Factories
{
    public class InMemoryImageFactory : IImageFactory
    {
        public static IImageFactory Instance { get; } = new InMemoryImageFactory();
        public IImage Create(int width, int height, ImageFormat format) => new InMemoryImage(width, height, format);
    }
}
