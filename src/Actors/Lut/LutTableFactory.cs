using System;
using ActorPipeline.Images2;

namespace ActorPipeline.Actors.Lut
{
    internal static class LutTableFactory
    {
        public static ILutTable Create(ImageFormat format, double contrast, double brightness, double gamma)
        {
            switch (format)
            {
                case ImageFormat.Grayscale8: return new LutTableGrayscale8Bit(contrast, brightness, gamma);
                case ImageFormat.Grayscale16: return new LutTableGrayscale16Bit(contrast, brightness, gamma);
                case ImageFormat.Bgra32: return new LutTableBgra32Bit(contrast, brightness, gamma);
                default:
                    throw new ArgumentException("Unsupported image format " + format, nameof(format));
            }
        }
    }
}
