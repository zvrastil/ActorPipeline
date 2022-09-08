using System;
using System.Runtime.InteropServices;

namespace ActorPipeline.Images2.Images
{
    public abstract class ImageBase : IImage
    {
        protected ImageBase(ImageSize size, ImageFormat format)
        {
            Width = size.Width;
            Height = size.Height;
            Format = format;
        }

        public int Width { get; }
        public int Height { get; }
        public ImageFormat Format { get; }

        public int BytesPerPixel
        {
            get
            {
                switch (Format)
                {
                    case ImageFormat.Grayscale8: return sizeof(byte);
                    case ImageFormat.Grayscale16: return sizeof(ushort);
                    case ImageFormat.Bgra32: return 4;
                    default: throw new NotSupportedException("Unsupported image format");
                }
            }
        }

        public ImageSize Size => new ImageSize(Width, Height);
        public IImageLineReadAccess LockLineForReading(int row)
        {
            if (row < 0 || row >= Height)
            {
                throw new ArgumentException($"Row {row} is out of bounds <0, {Height}).");
            }

            return LockLineForReadingImpl(row);
        }

        public IImageLineWriteAccess LockLineForWriting(int row)
        {
            if (row < 0 || row >= Height)
            {
                throw new ArgumentException($"Row {row} is out of bounds <0, {Height}).");
            }

            return LockLineForWritingImpl(row);
        }

        public void ReadRowPixels(int row, IntPtr data)
        {
            if (row < 0 || row >= Height)
            {
                throw new ArgumentException($"Row {row} is out of bounds <0, {Height}).");
            }
            using (var access = LockLineForReading(row))
            {
                CopyMemory(data, access.Ptr, (uint)Width);
            }
        }

        public void WriteRowPixels(int row, IntPtr data)
        {
            if (row < 0 || row >= Height)
            {
                throw new ArgumentException($"Row {row} is out of bounds <0, {Height}).");
            }
            using (var access = LockLineForWriting(row))
            {
                CopyMemory(access.Ptr, data, (uint)Width);
                access.Commit();
            }
        }


        protected abstract IImageLineReadAccess LockLineForReadingImpl(int row);

        protected abstract IImageLineWriteAccess LockLineForWritingImpl(int row);

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ImageBase()
        {
            Dispose(false);
        }

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        private static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);
    }
}
