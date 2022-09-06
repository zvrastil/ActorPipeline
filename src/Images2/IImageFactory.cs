namespace ActorPipeline.Images2
{
    public interface IImageFactory
    {
        IImage Create(int width, int height, ImageFormat format);
    }
}
