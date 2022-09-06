using System;

namespace ActorPipeline.Images2
{
    public interface IImage : IDisposable
    {
        int Width { get; }
        int Height { get; }
        ImageFormat Format { get; }
        int BytesPerPixel { get; }

        ImageSize Size { get; }

        IImageLineReadAccess LockLineForReading(int row);
        IImageLineWriteAccess LockLineForWriting(int row);

        void ReadRowPixels(int row, IntPtr data);
        void WriteRowPixels(int row, IntPtr data);
    }
}
