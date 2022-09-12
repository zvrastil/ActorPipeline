using System.Linq;
using ActorPipeline.Images2;

namespace ActorPipeline.Actors.Lut
{
    internal class LutTableGrayscale8Bit : LutTableBase
    {
        private readonly byte[] table;

        public LutTableGrayscale8Bit(double contrast, double brightness, double gamma)
            : base(contrast, brightness, gamma)
        {
            table = Enumerable
                .Range(0, 256)
                .Select(i => (byte) (255.0 * Transform(i / 255.0)))
                .ToArray();
        }

        public override byte[] ProcessData(byte[] data)
        {
            var result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = table[data[i]];
            }

            return result;
        }

        public override unsafe byte[] ProcessImage(IImage image)
        {
            var result = new byte[image.Width * image.Height];
            int index = 0;
            for (int y = 0; y < image.Height; y++)
            {
                using (var line = image.LockLineForReading(y))
                {
                    byte* srcRow = (byte*)line.Ptr;
                    for (int x = 0; x < image.Width; x++)
                    {
                        result[index++] = table[*(srcRow+x)];
                    }
                }
            }

            return result;
        }
    }
}
