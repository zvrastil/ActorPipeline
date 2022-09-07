using System;
using System.Linq;
using System.Threading.Tasks;
using ActorPipeline.Images2;
using ActorPipeline.Images2.Factories;

namespace ActorPipeline.Actors.ImageGenerators
{
    internal class MandelbrotImageGenerator : IImageGenerator
    {
        private readonly IImage targetImage;
        private readonly IImage sourceImage;
        private IPixelWriter pixelWriter;
        private int x;
        private int y;
        private int frame;
        private readonly sbyte[] noise;
        private int noiseIndex;
        private double noiseLevel;

        public MandelbrotImageGenerator(IImage targetImage, double noiseLevel)
        {
            this.targetImage = targetImage;
            this.sourceImage =
                InMemoryImageFactory.Instance.Create(targetImage.Size, targetImage.Format);
            GenerateImage(sourceImage);
            var random = new Random();
            noise = Enumerable.Range(0, 16361).Select(_ => (sbyte)(random.Next(255)-128)).ToArray();
            Reset(noiseLevel);
        }

        public void Dispose()
        {
            pixelWriter?.Dispose();
            sourceImage?.Dispose();
        }

        public ImagePosition Position => new ImagePosition(x, y);
        public int Frame => frame;
        public void Reset(double noiseLevel)
        {
            x = 0;
            y = 0;
            frame = 0;
            this.noiseLevel = noiseLevel;
            ResetPixelWriter();
        }

        private void ResetPixelWriter()
        {
            pixelWriter?.Dispose();
            pixelWriter = CreatePixelWriter(y);
        }

        private IPixelWriter CreatePixelWriter(int row)
        {
            switch (targetImage.Format)
            {
                case ImageFormat.Grayscale8: return new PixelWriter8Bit(sourceImage, targetImage, row);
                case ImageFormat.Grayscale16: return new PixelWriter16Bit(sourceImage, targetImage, row);
                case ImageFormat.Color24: return new PixelWriter24Bit(sourceImage, targetImage, row);
                default: throw new NotSupportedException("Unsupported image format");
            }
        }

        public void GenerateNextPixels(int numberOfPixels)
        {
            for (int i = 0; i < numberOfPixels; i++)
            {
                pixelWriter.NextPixel(NextNoise());
                x++;
                if (x >= targetImage.Width)
                {
                    x = 0;
                    y++;
                    if (y >= targetImage.Height)
                    {
                        y = 0;
                        frame++;
                    }
                    ResetPixelWriter();
                }
            }
        }

        private sbyte NextNoise()
        {
            sbyte result = unchecked ((sbyte)(noise[noiseIndex]*noiseLevel));
            noiseIndex = (noiseIndex + 1) % noise.Length;
            return result;
        }

        private static void GenerateImage(IImage image)
        {
            Parallel.ForEach(
                Enumerable.Range(0, image.Height),
                row => GenerateRow(image, row));
        }

        private static void GenerateRow(IImage image, int row)
        {
            using (var writer = CreateRowWriter(image, row))
            {
                writer.WriteRowPixels();
            }
        }

        private static IRowWriter CreateRowWriter(IImage image, int row)
        {
            switch (image.Format)
            {
                case ImageFormat.Grayscale8: return new Writer8bit(image, row);
                case ImageFormat.Grayscale16: return new Writer16bit(image, row);
                case ImageFormat.Color24: return new Writer24bit(image, row);
                default: throw new NotSupportedException("Unsupported image format " + image.Format);
            }
        }

        #region Image2ImageCopy

        private interface IPixelWriter : IDisposable
        {
            void NextPixel(sbyte noise);
        }

        private abstract class PixelWriterBase<T> : IPixelWriter where T : struct
        {
            protected readonly IImageLineReadAccess sourceAccess;
            protected readonly IImageLineWriteAccess targetAccess;

            protected PixelWriterBase(IImage sourceImage, IImage targetImage, int row)
            {
                sourceAccess = sourceImage.LockLineForReading(row);
                targetAccess = targetImage.LockLineForWriting(row);
            }

            public void Dispose()
            {
                sourceAccess?.Dispose();
                targetAccess?.Commit();
                targetAccess?.Dispose();
            }

            public abstract void NextPixel(sbyte noise);
        }

        private unsafe class PixelWriter8Bit : PixelWriterBase<byte>
        {
            private byte* srcPtr;
            private byte* dstPtr;

            public PixelWriter8Bit(IImage sourceImage, IImage targetImage, int row) : base(sourceImage, targetImage, row)
            {
                srcPtr = (byte*) sourceAccess.Ptr;
                dstPtr = (byte*) targetAccess.Ptr;
            }

            public override void NextPixel(sbyte noise)
            {
                *dstPtr = (byte)Math.Max(0, Math.Min(*srcPtr + noise, byte.MaxValue));
                dstPtr++;
                srcPtr++;
            }
        }

        private unsafe class PixelWriter16Bit : PixelWriterBase<ushort>
        {
            private ushort* srcPtr;
            private ushort* dstPtr;

            public PixelWriter16Bit(IImage sourceImage, IImage targetImage, int row) : base(sourceImage, targetImage, row)
            {
                srcPtr = (ushort*)sourceAccess.Ptr;
                dstPtr = (ushort*)targetAccess.Ptr;
            }

            public override void NextPixel(sbyte noise)
            {
                *dstPtr = (ushort) Math.Max(0, Math.Min(*srcPtr + noise * byte.MaxValue, ushort.MaxValue));
                dstPtr++;
                srcPtr++;
            }
        }

        private unsafe class PixelWriter24Bit : PixelWriterBase<RgbType>
        {
            private RgbType* srcPtr;
            private RgbType* dstPtr;

            public PixelWriter24Bit(IImage sourceImage, IImage targetImage, int row) : base(sourceImage, targetImage, row)
            {
                srcPtr = (RgbType*)sourceAccess.Ptr;
                dstPtr = (RgbType*)targetAccess.Ptr;
            }

            public override void NextPixel(sbyte noise)
            {
                var color = *srcPtr;
                color.R = (byte)Math.Max(0, Math.Min(color.R + noise, byte.MaxValue));
                color.G = (byte)Math.Max(0, Math.Min(color.G + noise, byte.MaxValue));
                color.B = (byte)Math.Max(0, Math.Min(color.B + noise, byte.MaxValue));
                *dstPtr = color;
                dstPtr++;
                srcPtr++;
            }
        }

        #endregion

        #region Mandelbrot Image Generation

        private interface IRowWriter : IDisposable
        {
            void WriteRowPixels();
        }


        private abstract class WriterMandelbrot<T> : IRowWriter where T : struct
        {
            protected const int kMax = 50;

            protected readonly IImageLineWriteAccess writeAccess;
            private readonly int width;
            private readonly int height;


            protected WriterMandelbrot(IImage targetImage, int row)
            {
                writeAccess = targetImage.LockLineForWriting(row);
                width = targetImage.Width;
                height = targetImage.Height;
            }

            public void Dispose()
            {
                writeAccess?.Dispose();
            }

            public void WriteRowPixels()
            {
                Complex c = new Complex(-2.0, -1.0 + writeAccess.Row * (2.0 / height));
                double rStep = 3.0 / width;
                for (int x = 0; x < width; x++)
                {
                    WriteNextColor(GetCurrentColor(c));
                    c.R += rStep;
                }
                writeAccess.Commit();
            }

            private T GetCurrentColor(Complex c)
            {
                Complex z = new Complex();
                double modulusSquared;
                int k = 0;
                do
                {
                    z = z.SquarePlusConst(c);
                    modulusSquared = z.ModulusSquared;
                    k++;
                } while (modulusSquared <= 4.0 && k < kMax);

                return CalculateColor(k);
            }

            protected abstract T CalculateColor(int k);

            protected abstract void WriteNextColor(T color);
        }

        private unsafe class Writer8bit : WriterMandelbrot<byte>
        {
            private byte* data;

            public Writer8bit(IImage targetImage, int row) : base(targetImage, row)
            {
                data = (byte*) writeAccess.Ptr;
            }


            protected override byte CalculateColor(int k) => (byte) (byte.MaxValue * (kMax - k) / kMax);

            protected override void WriteNextColor(byte color)
            {
                *data = color;
                data++;
            }
        }

        private unsafe class Writer16bit : WriterMandelbrot<ushort>
        {
            private ushort* data;

            public Writer16bit(IImage targetImage, int row) : base(targetImage, row)
            {
                data = (ushort*) writeAccess.Ptr;
            }


            protected override ushort CalculateColor(int k) => (ushort)(ushort.MaxValue * (kMax - k) / kMax);

            protected override void WriteNextColor(ushort color)
            {
                *data = color;
                data++;
            }
        }

        private unsafe class Writer24bit : WriterMandelbrot<RgbType>
        {
            private RgbType* data;
            private static readonly RgbType[] colorLookup;

            static Writer24bit()
            {
                colorLookup = Enumerable
                    .Range(0, kMax)
                    .Select(k => HSLtoRGB((double) k / kMax, 1.0, 0.5))
                    .Concat(new[] {new RgbType(0,0,0)})
                    .ToArray();

            }


            public Writer24bit(IImage targetImage, int row) : base(targetImage, row)
            {
                data = (RgbType*) writeAccess.Ptr;
            }


            protected override RgbType CalculateColor(int k) => colorLookup[k];

            protected override void WriteNextColor(RgbType color)
            {
                *data = color;
                data++;
            }

            private static RgbType HSLtoRGB(double H, double S, double L)
            {
                double v;
                double r, g, b;

                r = L;   // Set RGB all equal to L, defaulting to grey.
                g = L;
                b = L;

                // Standard HSL to RGB conversion. This is described in
                // detail at:
                // http://www.niwa.nu/2013/05/math-behind-colorspace-conversions-rgb-hsl/
                v = (L <= 0.5) ? (L * (1.0 + S)) : (L + S - L * S);

                if (v > 0)
                {
                    double m;
                    double sv;
                    int sextant;
                    double fract, vsf, mid1, mid2;

                    m = L + L - v;
                    sv = (v - m) / v;
                    H *= 6.0;
                    sextant = (int)H;
                    fract = H - sextant;
                    vsf = v * sv * fract;
                    mid1 = m + vsf;
                    mid2 = v - vsf;

                    switch (sextant)
                    {
                        case 0:
                            r = v;
                            g = mid1;
                            b = m;
                            break;

                        case 1:
                            r = mid2;
                            g = v;
                            b = m;
                            break;

                        case 2:
                            r = m;
                            g = v;
                            b = mid1;
                            break;

                        case 3:
                            r = m;
                            g = mid2;
                            b = v;
                            break;

                        case 4:
                            r = mid1;
                            g = m;
                            b = v;
                            break;

                        case 5:
                            r = v;
                            g = m;
                            b = mid2;
                            break;
                    }
                }

                // Create Color object from RGB values.
                return new RgbType((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
            }
        }

        private struct Complex
        {
            public double R { get; set; }
            public double I { get; set; }

            public Complex(double r, double i)
            {
                R = r;
                I = i;
            }

            public Complex SquarePlusConst(Complex arg)
            {
                Complex result = new Complex();
                result.R = R * R - I * I;
                result.I = 2 * R * I;
                result.R += arg.R;
                result.I += arg.I;
                return result;
            }

            public double ModulusSquared => R * R + I * I;
        }

        #endregion
    }
}
