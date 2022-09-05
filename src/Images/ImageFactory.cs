using System;

namespace ActorPipeline.Images
{
    public static class ImageFactory
    {
        public static IImage Create(int width, int height, ImageFormat format)
        {
            switch (format)
            {
                case ImageFormat.Grayscale8: return new Image<byte>(width, height, format);
                case ImageFormat.Grayscale16: return new Image<ushort>(width, height, format);
                case ImageFormat.Color24: return new Image<RgbType>(width, height, format);
                default: throw new ArgumentException($"Unknown image format {format}", nameof(format));
            }
        }                    
    }
}
