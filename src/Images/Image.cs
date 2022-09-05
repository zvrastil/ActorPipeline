using System;
using System.Runtime.InteropServices;

namespace ActorPipeline.Images
{
    public class Image<T> : IImage where T : struct
    {
        private readonly int dataSize;
        public Image(int width, int height, ImageFormat format)
        {
            CheckFormat(format);
            Width = width;
            Height = height;
            Format = format;
            dataSize = Width * Height * BytesPerPixel;
            GC.AddMemoryPressure(dataSize);
            Data = Marshal.AllocHGlobal(dataSize);
        }

        ~Image()
        {
            if (Data != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(Data);
                GC.RemoveMemoryPressure(dataSize);
            }
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
                    case ImageFormat.Grayscale8: return 1;
                    case ImageFormat.Grayscale16: return 2;
                    case ImageFormat.Color24: return 3;
                    default: throw new NotSupportedException();
                }
            }
        }            

        public IntPtr Data { get; }

        public byte[] ExtractData(IntRectangle rect)
        {
            var data = new byte[rect.Width * rect.Height * BytesPerPixel];
            if (rect.Left == 0 && rect.Width == Width)
            {
                Marshal.Copy(Data + rect.Top * Width * BytesPerPixel, data, 0, data.Length);
            }
            else
            {
                for (int y = 0; y < rect.Height; y++)
                {
                    Marshal.Copy(Data + (y + rect.Top) * Width * BytesPerPixel, data, y * rect.Width, rect.Width * BytesPerPixel);
                }
            }
            return data;
        }

        private void CheckFormat(ImageFormat format)
        {
            switch (format)
            {
                case ImageFormat.Grayscale8: RequireImageType<byte>(format); break;
                case ImageFormat.Grayscale16: RequireImageType<ushort>(format); break;
                case ImageFormat.Color24: RequireImageType<RgbType>(format); break;
                default: throw new ArgumentException("Unknown image format: " + format);
            }
        }

        private void RequireImageType<TCheck>(ImageFormat format)
        {
            if (typeof(T) != typeof(TCheck))
            {
                throw new ArgumentException($"Specified image format {format} does not match generic type {typeof(T).Name}");
            }
        }
    }
}
