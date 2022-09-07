namespace ActorPipeline.Images2
{
    public interface IImageFactory
    {
        IImage Create(ImageSize size, ImageFormat format);
    }
}
