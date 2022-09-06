using System;
using System.Runtime.InteropServices;

namespace ActorPipeline.Images2.Images
{
    public class InMemoryImage : ImageBase, IContinuousImage
    {
        public InMemoryImage(int width, int height, ImageFormat format) : base(width, height, format)
        {
            this.ImageData = Marshal.AllocHGlobal(Width * Height * BytesPerPixel);
        }

        public IntPtr ImageData { get; }

        public int Stride => Width*BytesPerPixel;

        protected override IImageLineReadAccess LockLineForReadingImpl(int row) =>
            new LineAccess(this, row);

        protected override IImageLineWriteAccess LockLineForWritingImpl(int row) =>
            new LineAccess(this, row);

        protected override void Dispose(bool disposing)
        {
            Marshal.FreeHGlobal(ImageData);
            base.Dispose(disposing);
        }

        private class LineAccess : IImageLineReadAccess, IImageLineWriteAccess
        {

            public LineAccess(InMemoryImage image, int row)
            {
                Row = row;
                Ptr = image.ImageData + row * image.Stride;
            }

            public int Row { get; }
            public IntPtr Ptr { get; }
            public void Commit()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
