using ActorPipeline.Images2;
using ActorPipeline.Messages;
using Akka.Actor;
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
        private Image imageControl;
        private FrameworkElement imageContainer;
        private IActorRef imageDisplayPipeline;
        private WriteableBitmap bitmap;
        private int bytesPerPixel;
        private long currentFormatId;
        private long appliedFormatId;
        

        public sealed class SetImageControlMessage
        {
            public SetImageControlMessage(Image imageControl, FrameworkElement imageContainer)
            {
                ImageControl = imageControl;
                ImageContainer = imageContainer;
            }

            public Image ImageControl { get; }
            public FrameworkElement ImageContainer { get; }

            public override string ToString() => $"WpfImageControlUpdaterActor_SetImageControl({ImageControl?.Name}, {ImageContainer?.Name})";
        }       

        public WpfImageControlUpdaterActor() : base(FilterFlags.TargetFilter)
        {
            BaseMessages();
            Receive<SetImageControlMessage>(HandleImageControlChange);
            Receive<DisplayMessages.SetImageDisplayPipelineMessage>(HandleImageDisplayPipelineChange);
            Receive<PipelineFilterMessages.FormatChangedMessage>(HandleFormatChange);
            Receive<PipelineFilterMessages.PartialUpdateMessage>(HandleImageUpdate);            
        }

        private void HandleImageDisplayPipelineChange(DisplayMessages.SetImageDisplayPipelineMessage message)
        {
            LogMessage(message);
            imageDisplayPipeline = message.Pipeline;
            UpdateDisplaySize();
        }

        private void HandleImageControlChange(SetImageControlMessage message)
        {
            LogMessage(message);

            if (imageControl != null)
            {                
                imageControl.Source = null;
            }
            if (imageContainer != null)
            {
                imageContainer.SizeChanged -= ImageContainer_SizeChanged;
            }

            imageControl = message.ImageControl;
            imageContainer = message.ImageContainer;
            imageContainer.SizeChanged += ImageContainer_SizeChanged;
            
            UpdateDisplaySize();

            if (bitmap != null)
            {
                imageControl.Source = bitmap;
                appliedFormatId = currentFormatId;
            }
        }

        private void ImageContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateDisplaySize();
        }

        private void UpdateDisplaySize()
        {
            if (imageDisplayPipeline != null && imageContainer != null &&
                imageContainer.ActualWidth > 0 && imageContainer.ActualHeight > 0)
            {
                imageDisplayPipeline?.Tell(
                    new DisplayMessages.SetDisplaySizeMessage(new ImageSize((int)imageContainer.ActualWidth, (int)imageContainer.ActualHeight)));
            }
        }

        private void HandleFormatChange(PipelineFilterMessages.FormatChangedMessage message)
        {
            LogMessage(message);
            var (format, bytesPerPixel) = GetPixelFormat(message.Format);
            bitmap = new WriteableBitmap(message.Size.Width, message.Size.Height, 96.0, 96.0, format, null);    
            this.bytesPerPixel = bytesPerPixel;
            ClearBitmap(bitmap);
            currentFormatId = message.Id;
            // do not set the source immediately - postpone it to the first update to prevent
            // display from going black
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
            if (message.FormatChangeId != currentFormatId)
            {
                return;
            }
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

            if (imageControl != null && appliedFormatId != currentFormatId)
            {
                imageControl.Source = bitmap;
                appliedFormatId = currentFormatId;
            }
        }        

        private static (PixelFormat pixelFormat, int bytesPerPixel) GetPixelFormat(ImageFormat format)
        {
            switch (format)
            {
                case ImageFormat.Grayscale8: return (PixelFormats.Gray8, 1);
                case ImageFormat.Grayscale16: return (PixelFormats.Gray16, 2);
                case ImageFormat.Bgra32: return (PixelFormats.Bgr32, 4);
                default: throw new ArgumentException($"Unknown pixel format {format}", nameof(format));
            }
        }

        private static Int32Rect ToInt32Rect(ImageRegion changed) =>
            new Int32Rect(changed.Left, changed.Top, changed.Width, changed.Height);

        [DllImport("kernel32.dll", EntryPoint = "RtlFillMemory", SetLastError = false)]
        static extern void FillMemory(IntPtr destination, uint length, byte fill);

    }
}
