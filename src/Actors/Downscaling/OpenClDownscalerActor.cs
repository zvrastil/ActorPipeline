using System;
using ActorPipeline.Images2;
using ActorPipeline.Messages;
using Akka.Actor;
using Cloo;

namespace ActorPipeline.Actors.Downscaling
{
    internal class OpenClDownscalerActor : ReceiveActor
    {
        private readonly IActorRef parentDownscaler;
        private readonly ComputeContext openClContext;
        private ComputeCommandQueue openClCommandQueue;
        private ComputeImage2D inputBuffer;
        private ImageFormat currentFormat;

        public OpenClDownscalerActor(IActorRef parentDownscaler, ComputeContext openClContext)
        {
            this.parentDownscaler = parentDownscaler;
            this.openClContext = openClContext;
            openClCommandQueue =
                new ComputeCommandQueue(openClContext, openClContext.Devices[0], ComputeCommandQueueFlags.None);

            Receive<DownscalingMessages.SetDownscalingSettingsMessage>(HandleSettingsChange);
            Receive<PipelineFilterMessages.PartialUpdateMessage>(HandlePartialUpdate);
        }

        protected override void PostStop()
        {
            openClCommandQueue?.Dispose();
            openClCommandQueue = null;
            base.PostStop();
        }

        private void HandlePartialUpdate(PipelineFilterMessages.PartialUpdateMessage message)
        {
            PerformPartialResize(message);
        }

        private void HandleSettingsChange(DownscalingMessages.SetDownscalingSettingsMessage message)
        {
            bool needFullResize = false;

            if (InputBufferFormatChanged(message))
            {
                CreateNewInputBuffer(message);
            }

            if (DisplaySizeChanged(message))
            {
                CreateNewOutputBuffer(message);
                needFullResize = true;
            }

            if (ZoomedRegionChanged(message))
            {
                needFullResize = true;
            }

            if (needFullResize)
            {
                PerformFullResize();
            }
        }


        private bool InputBufferFormatChanged(DownscalingMessages.SetDownscalingSettingsMessage message)
        {
            return (inputBuffer == null ||
                    message.Format != currentFormat ||
                    message.InputWidth != inputBuffer.Width ||
                    message.InputHeight != inputBuffer.Height);
        }

        private void CreateNewInputBuffer(DownscalingMessages.SetDownscalingSettingsMessage message)
        {
#pragma warning disable CS0618
            inputBuffer = new ComputeImage2D(openClContext, ComputeMemoryFlags.ReadOnly, GetBufferFormat(message),
                message.InputWidth, message.InputHeight, 0, IntPtr.Zero);
            currentFormat = message.Format;
#pragma warning restore CS0618
        }

        private void CreateNewOutputBuffer(DownscalingMessages.SetDownscalingSettingsMessage message)
        {
            throw new NotImplementedException();
        }

        private static ComputeImageFormat GetBufferFormat(
            DownscalingMessages.SetDownscalingSettingsMessage message)
        {
            switch (message.Format)
            {
                case ImageFormat.Grayscale8:
                    return new ComputeImageFormat(ComputeImageChannelOrder.Intensity,
                        ComputeImageChannelType.UnsignedInt8);
                case ImageFormat.Grayscale16:
                    return new ComputeImageFormat(ComputeImageChannelOrder.Intensity,
                        ComputeImageChannelType.UnsignedInt16);
                case ImageFormat.Color24:
                    return new ComputeImageFormat(ComputeImageChannelOrder.Rgb,
                        ComputeImageChannelType.UnsignedInt8);
                default:
                    throw new NotSupportedException("Unsupported image format");
            }
        }
    }
}
