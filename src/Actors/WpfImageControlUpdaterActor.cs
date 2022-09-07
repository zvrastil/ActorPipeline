using ActorPipeline.Images2;
using ActorPipeline.Messages;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ActorPipeline.Actors
{
    public sealed class WpfImageControlUpdaterActor : FilterActorBase
    {
        private readonly Image imageControl;
        private WriteableBitmap bitmap;
        private int bytesPerPixel;

        public WpfImageControlUpdaterActor(Image imageControl) : base(FilterFlags.TargetFilter)
        {
            this.imageControl = imageControl;
            BaseMessages();
            Receive<PipelineFilterMessages.FormatChangedMessage>(HandleFormatChange);
            Receive<PipelineFilterMessages.PartialUpdateMessage>(HandleImageUpdate);            
        }

        private void HandleFormatChange(PipelineFilterMessages.FormatChangedMessage message)
        {
            LogMessage(message);
            var (format, bytesPerPixel) = GetPixelFormat(message.Format);
            bitmap = new WriteableBitmap(message.Size.Width, message.Size.Height, 96.0, 96.0, format, null);    
            this.bytesPerPixel = bytesPerPixel;
            ClearBitmap(bitmap);
            imageControl.Source = bitmap;
        }

        private static void ClearBitmap(WriteableBitmap bitmap)
        {
            bitmap.Lock();
            try
            {
                FillMemory(bitmap.BackBuffer, (uint)(bitmap.PixelHeight * bitmap.BackBufferStride), 0);
                bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
            }
            finally
            {
                bitmap.Unlock();
            }
               
        }

        private void HandleImageUpdate(PipelineFilterMessages.PartialUpdateMessage message)
        {
            LogMessage(message);
            if (bitmap == null)
            {
                return;
            }
            bitmap.Lock();
            try
            {
                for (int row = 0; row < message.Changed.Height; row++)
                {
                    IntPtr linePtr = bitmap.BackBuffer + (message.Changed.Top+row) * bitmap.BackBufferStride;
                    Marshal.Copy(message.Data, row * message.Changed.Width * bytesPerPixel, linePtr + message.Changed.Left * bytesPerPixel, message.Changed.Width * bytesPerPixel);
                }
                bitmap.AddDirtyRect(ToInt32Rect(message.Changed));
            }
            finally
            {
                bitmap.Unlock();
            }
        }        

        private static (PixelFormat pixelFormat, int bytesPerPixel) GetPixelFormat(ImageFormat format)
        {
            switch (format)
            {
                case ImageFormat.Grayscale8: return (PixelFormats.Gray8, 1);
                case ImageFormat.Grayscale16: return (PixelFormats.Gray16, 2);
                case ImageFormat.Color24: return (PixelFormats.Rgb24, 3);
                default: throw new ArgumentException($"Unknown pixel format {format}", nameof(format));
            }
        }

        private static Int32Rect ToInt32Rect(ImageRegion changed) =>
            new Int32Rect(changed.Left, changed.Top, changed.Width, changed.Height);

        [DllImport("kernel32.dll", EntryPoint = "RtlFillMemory", SetLastError = false)]
        static extern void FillMemory(IntPtr destination, uint length, byte fill);

    }
}
