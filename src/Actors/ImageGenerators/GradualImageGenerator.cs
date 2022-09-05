using ActorPipeline.Images;
using System;

namespace ActorPipeline.Actors.ImageGenerators
{
    internal class GradualImageGenerator : IImageGenerator
    {
        private readonly IImage targetImage;
        private long currentPosition;
        private readonly long dataSize;
        private IWriter writer;

        public GradualImageGenerator(IImage targetImage)
        {
            this.targetImage = targetImage;
            dataSize = targetImage.Width * targetImage.Height;
            Reset();           
        }

        private IWriter CreateWriter()
        {
            switch (targetImage.Format)
            {
                case ImageFormat.Grayscale8: return new Writer8Bit(targetImage);
                case ImageFormat.Grayscale16: return new Writer16Bit(targetImage);
                case ImageFormat.Color24: return new WriterRgb(targetImage);
                default: throw new NotSupportedException();
            }
        }

        public IntPosition Position { get; private set; }

        public int Frame { get; private set; }

        public void GenerateNextPixels(int numberOfPixels)
        {
            for (int i = 0; i < numberOfPixels; i++)
            {
                writer.WriteNext();
                currentPosition++;
                if (currentPosition >= dataSize)
                {
                    currentPosition = 0;
                    Frame++;
                }
            }
            Position = new IntPosition((int)(currentPosition % targetImage.Width), (int)(currentPosition / targetImage.Width));
        }

        public void Reset()
        {
            currentPosition = 0;
            Frame = 0;
            writer = CreateWriter();            
        }

        private interface IWriter
        {
            void WriteNext();
        }

        private unsafe class Writer8Bit : IWriter
        {
            private readonly IImage image;
            private int x;
            private int y;
            private byte* current;
            private byte nextValue;
            private int frame;

            public Writer8Bit(IImage image)
            {                
                this.image = image;
                current = (byte*)image.Data;
                x = 0;
                y = 0;
                frame = 0;
                nextValue = 0;
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
                        current = (byte*)image.Data;
                        frame++;
                    }
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
            private ushort* current;
            private ushort nextValue;
            private int frame;

            public Writer16Bit(IImage image)
            {
                this.image = image;
                current = (ushort*)image.Data;
                x = 0;
                y = 0;
                frame = 0;
                nextValue = 0;
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
                        current = (ushort*)image.Data;
                        frame++;
                    }
                    nextValue = (ushort)(((y+frame) % 256) << 8);
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
            private byte* current;
            private byte nextValue;
            private int nextChannel;
            private int frame = -1;

            public WriterRgb(IImage image)
            {
                this.image = image;
                current = (byte*)image.Data;
                x = 0;
                y = 0;
                frame = 0;
                nextValue = 0;
                nextChannel = 0;
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
                        current = (byte*)image.Data;
                        frame++;
                    }
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
