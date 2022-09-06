using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ActorPipeline.Images2.Images
{
    public enum RawFileImageMode
    {
        Read,
        Create
    }

    public class RawFileImage : ImageBase
    {
        private readonly FileStream fileStream;
        private readonly object fileStreamLock = new object();

        public static RawFileImage OpenExisting(string filePath, int width, int height, ImageFormat format) =>
            new RawFileImage(RawFileImageMode.Read, filePath, width, height, format);

        public static RawFileImage CreateNew(string filePath, int width, int height, ImageFormat format) =>
            new RawFileImage(RawFileImageMode.Create, filePath, width, height, format);

        protected RawFileImage(RawFileImageMode mode, string filePath, int width, int height, ImageFormat format) : base(width, height, format)
        {
            if (mode == RawFileImageMode.Read)
            {
                if (!File.Exists(filePath))
                {
                    throw new ArgumentException($"File '{filePath}' not found.", nameof(filePath));
                }

                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length != ImageSize)
                {
                    throw new ArgumentException(
                        $"File '{filePath}' length {fileInfo.Length} does not match expected length of {width * height}.");
                }
            }

            fileStream = new FileStream(filePath, mode == RawFileImageMode.Read ? FileMode.Open : FileMode.Create);
            if (mode == RawFileImageMode.Create)
            {
                fileStream.SetLength(ImageSize);
            }

        }

        private long RowSize => Width * BytesPerPixel;

        private long ImageSize => RowSize * Height;

        protected override IImageLineReadAccess LockLineForReadingImpl(int row) =>
            new FileLineAccess(this, row);

        protected override IImageLineWriteAccess LockLineForWritingImpl(int row) =>
            new FileLineAccess(this, row);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                fileStream.Dispose();
            }
        }

        private sealed class FileLineAccess : IImageLineReadAccess, IImageLineWriteAccess
        {
            private readonly RawFileImage image;
            private readonly byte[] lineBuffer;
            private GCHandle lineBufferPin;

            public FileLineAccess(RawFileImage image, int row)
            {
                this.image = image;
                Row = row;
                lineBuffer = new byte[image.RowSize];
                lock (image.fileStreamLock)
                {
                    image.fileStream.Seek(image.RowSize * row, SeekOrigin.Begin);
                    image.fileStream.Read(lineBuffer, 0, lineBuffer.Length);
                }
                lineBufferPin = GCHandle.Alloc(lineBuffer, GCHandleType.Pinned);
                Ptr = lineBufferPin.AddrOfPinnedObject();
            }

            public int Row { get; }
            public IntPtr Ptr { get; }
            public void Commit()
            {
                lock (image.fileStreamLock)
                {
                    image.fileStream.Seek(image.RowSize * Row, SeekOrigin.Begin);
                    image.fileStream.Write(lineBuffer, 0, lineBuffer.Length);
                }
            }

            private void ReleaseUnmanagedResources()
            {
                if (lineBufferPin.IsAllocated)
                {
                    lineBufferPin.Free();
                }
            }

            public void Dispose()
            {
                ReleaseUnmanagedResources();
                GC.SuppressFinalize(this);
            }

            ~FileLineAccess()
            {
                ReleaseUnmanagedResources();
            }
        }

        
    }
}
