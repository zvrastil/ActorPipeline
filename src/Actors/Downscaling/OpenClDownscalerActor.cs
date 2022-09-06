using System;
using System.IO;
using System.Reflection;
using ActorPipeline.Images2;
using ActorPipeline.Messages;
using Akka.Actor;
using Cloo;

namespace ActorPipeline.Actors.Downscaling
{
    internal unsafe class OpenClDownscalerActor : ReceiveActor
    {
        private readonly IActorRef parentDownscaler;
        private readonly ComputeContext clContext;
        private ComputeCommandQueue clCommandQueue;
        private ComputeProgram clProgram;
        private ComputeKernel clKernel;
        private ComputeImage2D inputBuffer;
        private ComputeImage2D outputBuffer;
        private ImageFormat currentFormat;
        private ImageRegion inputRegion;

        public OpenClDownscalerActor(IActorRef parentDownscaler, ComputeContext clContext)
        {
            this.parentDownscaler = parentDownscaler;
            this.clContext = clContext;
            clCommandQueue =
                new ComputeCommandQueue(clContext, clContext.Devices[0], ComputeCommandQueueFlags.None);

            clProgram = new ComputeProgram(this.clContext, new string[] { GetProgramCode() });
            try
            {
                clProgram.Build(null, null, null, IntPtr.Zero);
            }
            catch (BuildProgramFailureComputeException)
            {
                throw new ApplicationException(clProgram.GetBuildLog(this.clContext.Devices[0]));
            }
            clKernel = clProgram.CreateKernel("resize");

            Receive<DownscalingMessages.SetDownscalingSettingsMessage>(HandleSettingsChange);
            Receive<PipelineFilterMessages.PartialUpdateMessage>(HandlePartialUpdate);
        }

        protected override void PostStop()
        {
            inputBuffer?.Dispose();
            inputBuffer = null;
            outputBuffer?.Dispose();
            outputBuffer = null;
            clKernel?.Dispose();
            clKernel = null;
            clProgram?.Dispose();
            clProgram = null;
            clCommandQueue?.Dispose();
            clCommandQueue = null;

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

            if (OutputSizeChanged(message))
            {
                CreateNewOutputBuffer(message);
                needFullResize = true;
            }

            if (ZoomedRegionChanged(message))
            {
                inputRegion = CalculateInputRegion(message);
                needFullResize = true;
            }

            if (needFullResize)
            {
                PerformFullResize();
            }
        }

        private void PerformFullResize()
        {
            CLSourceRegion sourceRegion = new CLSourceRegion { Left = inputRegion.Left, Top = inputRegion.Top, Width = inputRegion.Width, Height = inputRegion.Height };
            CLImageSize outputSize = new CLImageSize { Width = (uint)outputBuffer.Width, Height = (uint)outputBuffer.Height };
            clKernel.SetMemoryArgument(0, inputBuffer);
            clKernel.SetMemoryArgument(1, outputBuffer);
            clKernel.SetValueArgument(2, sourceRegion);
            clKernel.SetValueArgument(3, outputSize);
            clCommandQueue.Execute(clKernel, null, new long[] {outputBuffer.Width, outputBuffer.Height}, null, null);

            byte[] result = new byte[outputBuffer.Height * outputBuffer.RowPitch];
            fixed (byte* ptr = result)
            {
                clCommandQueue.ReadFromImage(outputBuffer, new IntPtr(ptr), true, null);
            }
            parentDownscaler.Tell(new )
        }

        private bool ZoomedRegionChanged(DownscalingMessages.SetDownscalingSettingsMessage message)
        {
            ImageRegion newInputRegion = CalculateInputRegion(message);
            return inputRegion != newInputRegion;
        }

        private static ImageRegion CalculateInputRegion(DownscalingMessages.SetDownscalingSettingsMessage message)
        {
            var mustSeeRegion = message.ZoomedArea.ToPixels(new ImageSize(message.InputWidth, message.InputHeight));
            double scale = Math.Min((double)message.DisplayWidth / mustSeeRegion.Width, (double)message.DisplayHeight / mustSeeRegion.Height);
            ImageSize idealSize = new ImageSize((int)(message.DisplayWidth / scale + 0.5), (int)(message.DisplayHeight / scale + 0.5));
            return mustSeeRegion.ExpandSafelyTo(idealSize, new ImageSize(message.InputWidth, message.InputHeight));
        }

        private bool OutputSizeChanged(DownscalingMessages.SetDownscalingSettingsMessage message)
        {
            ImageSize imageSize = CalculateOutputSize(message);
            return outputBuffer == null ||
                   outputBuffer.Width != imageSize.Width ||
                   outputBuffer.Height != imageSize.Height;
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
            inputBuffer = new ComputeImage2D(clContext, ComputeMemoryFlags.ReadOnly, GetBufferFormat(message),
                message.InputWidth, message.InputHeight, message.InputWidth*GetBytesPerPixel(message), IntPtr.Zero);
#pragma warning restore CS0618
            currentFormat = message.Format;
        }

        private void CreateNewOutputBuffer(DownscalingMessages.SetDownscalingSettingsMessage message)
        {
            ImageSize imageSize = CalculateOutputSize(message);
#pragma warning disable CS0618
            outputBuffer = new ComputeImage2D(clContext, ComputeMemoryFlags.WriteOnly, GetBufferFormat(message),
                imageSize.Width, imageSize.Height, imageSize.Width*GetBytesPerPixel(message), IntPtr.Zero);
#pragma warning restore CS0618
        }

        private ImageSize CalculateOutputSize(DownscalingMessages.SetDownscalingSettingsMessage message)
        {
            ImageSize inputRegionSize = CalculateInputRegion(message).Size;
            double scale = Math.Min((double)message.DisplayWidth / inputRegionSize.Width, (double)message.DisplayHeight / inputRegionSize.Height);
            if (scale >= 1.0)
            {
                return inputRegionSize;
            }
            else
            {
                return new ImageSize((int)(scale * inputRegionSize.Width + 0.5), (int)(scale * inputRegionSize.Height + 0.5));
            }
        }

        private static long GetBytesPerPixel(DownscalingMessages.SetDownscalingSettingsMessage message)
        {
            switch (message.Format)
            {
                case ImageFormat.Grayscale8: return 1;
                case ImageFormat.Grayscale16: return 2;
                case ImageFormat.Color24: return 3;
                default:
                    throw new NotSupportedException("Unsupported image format");
            }
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

        private string GetProgramCode()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var streamReader = new StreamReader(assembly.GetManifestResourceStream("ActorPipeline.Actors.Downscaling.DownscalingKernels.cl")))
            {
                return streamReader.ReadToEnd();
            }
        }

        private struct CLSourceRegion
        {
            public float Left;
            public float Top;
            public float Width;
            public float Height;
        };

        private struct CLImageSize
        {
            public uint Width;
            public uint Height;
        };
    }
}
