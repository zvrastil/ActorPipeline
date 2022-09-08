using ActorPipeline.Images2;
using System;

namespace ActorPipeline.Actors.ImageGenerators
{
    internal class GradualImageGenerator : BaseImageGenerator
    {
        public GradualImageGenerator(IImage targetImage) : base(targetImage)
        {
        }

        protected override IWriter CreateWriter()
        {
            switch (targetImage.Format)
            {
                case ImageFormat.Grayscale8: return new Writer8Bit(targetImage);
                case ImageFormat.Grayscale16: return new Writer16Bit(targetImage);
                case ImageFormat.Bgra32: return new WriterRgb(targetImage);
                default: throw new NotSupportedException();
            }
        }

        private unsafe class Writer8Bit : IWriter
        {
            private readonly IImage image;
            private int x;
            private int y;
            private IImageLineWriteAccess lineAccess;
            private byte* current;
            private byte nextValue;
            private int frame;

            public Writer8Bit(IImage image)
            {                
                this.image = image;
                x = 0;
                y = 0;
                frame = 0;
                nextValue = 0;
                lineAccess = image.LockLineForWriting(y);
                current = (byte*) lineAccess.Ptr;
            }

            public void WriteNext()
            {
                *current = nextValue;
                current++;
                x++;
                if (x == image.Width)
                {
                    x = 0;
                    y++;
                    if (y == image.Height)
                    {
                        y = 0;
                        frame++;
                    }
                    lineAccess.Commit();
                    lineAccess.Dispose();
                    lineAccess = image.LockLineForWriting(y);
                    current = (byte*)lineAccess.Ptr;
                    nextValue = (byte)((y+frame) % 256);
                }
                else
                {
                    unchecked { nextValue++; }
                }
            }
        }

        private unsafe class Writer16Bit : IWriter
        {
            private readonly IImage image;
            private int x;
            private int y;
            private IImageLineWriteAccess lineAccess;
            private ushort* current;
            private ushort nextValue;
            private int frame;

            public Writer16Bit(IImage image)
            {
                this.image = image;
                x = 0;
                y = 0;
                frame = 0;
                nextValue = 0;
                lineAccess = image.LockLineForWriting(y);
                current = (ushort*)lineAccess.Ptr;
            }

            public void WriteNext()
            {
                *current = nextValue;
                current++;
                x++;
                if (x == image.Width)
                {
                    x = 0;
                    y++;
                    if (y == image.Height)
                    {
                        y = 0;
                        frame++;
                    }
                    lineAccess.Commit();
                    lineAccess.Dispose();
                    lineAccess = image.LockLineForWriting(y);
                    current = (ushort*)lineAccess.Ptr;
                    nextValue = (ushort)(((y + frame) % 256) << 8);
                }
                else
                {
                    unchecked { nextValue += 256; }
                }
            }
        }

        private unsafe class WriterRgb : IWriter
        {
            private readonly IImage image;
            private int x;
            private int y;
            private IImageLineWriteAccess lineAccess;
            private byte* current;
            private byte nextValue;
            private int nextChannel;
            private int frame;

            public WriterRgb(IImage image)
            {
                this.image = image;
                x = 0;
                y = 0;
                frame = 0;
                nextValue = 0;
                nextChannel = 0;
                lineAccess = image.LockLineForWriting(y);
                current = (byte*)lineAccess.Ptr;
            }

            public void WriteNext()
            {
                *current = (byte)(nextChannel == 0 ? nextValue : 0);
                *(current + 1) = (byte)(nextChannel == 1 ? nextValue : 0);
                *(current + 2) = (byte)(nextChannel == 2 ? nextValue : 0);
                current += 3;
                x++;
                if (x == image.Width)
                {
                    x = 0;
                    y++;
                    if (y == image.Height)
                    {
                        y = 0;
                        frame++;
                    }
                    lineAccess.Commit();
                    lineAccess.Dispose();
                    lineAccess = image.LockLineForWriting(y);
                    current = (byte*)lineAccess.Ptr;
                    nextValue = (byte)((y+frame) % 256);
                    nextChannel = ((y+frame) / 768) % 3;
                }
                else
                {
                    if (nextValue == 255)
                    {
                        nextValue = 0;
                        nextChannel = (nextChannel + 1) % 3;
                    }
                    else
                    {
                        nextValue++;
                    }
                }
            }
        }
    }
}
