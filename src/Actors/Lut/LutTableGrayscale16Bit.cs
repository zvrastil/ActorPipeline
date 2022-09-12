using ActorPipeline.Images2;
using System.Linq;

namespace ActorPipeline.Actors.Lut
{
    internal class LutTableGrayscale16Bit : LutTableBase
    {
        private readonly ushort[] table;

        public LutTableGrayscale16Bit(double contrast, double brightness, double gamma)
            : base(contrast, brightness, gamma)
        {
            table = Enumerable
                .Range(0, 65536)
                .Select(i => (ushort)(65535.0 * Transform(i / 65535.0)))
                .ToArray();
        }

        public override unsafe byte[] ProcessData(byte[] data)
        {
            var result = new byte[data.Length];
            fixed (byte* srcPtrByte = data)
            fixed(byte* dstPtrByte = result)
            {
                ushort* srcPtr = (ushort*) srcPtrByte;
                ushort* dstPtr = (ushort*) dstPtrByte;

                for (int i = 0; i < data.Length/2; i++)
                {
                    *dstPtr = table[*srcPtr];
                    dstPtr++;
                    srcPtr++;
                }
            }

            return result;
        }

        public override unsafe byte[] ProcessImage(IImage image)
        {
            var result = new byte[image.Width * image.Height * image.BytesPerPixel];

            fixed (byte* dstPtrByte = result)
            {
                ushort* dstPtr = (ushort*) dstPtrByte;

                for (int y = 0; y < image.Height; y++)
                {
                    using (var line = image.LockLineForReading(y))
                    {
                        ushort* srcPtr = (ushort*) line.Ptr;
                        for (int x = 0; x < image.Width; x++)
                        {
                            *dstPtr = table[*srcPtr];
                            dstPtr++;
                            srcPtr++;
                        }
                    }
                }
            }

            return result;
        }
    }
}
