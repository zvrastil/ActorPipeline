using System;

namespace ActorPipeline.Images
{
    public interface IImage
    {
        ImageFormat Format { get; }
        int Height { get; }
        int Width { get; }        
        IntPtr Data { get; }
        byte[] ExtractData(IntRectangle rect);
    }
}