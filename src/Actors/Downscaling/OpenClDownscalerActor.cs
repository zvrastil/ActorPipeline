using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using ActorPipeline.Images2;
using ActorPipeline.Messages;
using Akka.Actor;
using Cloo;
using Serilog;

namespace ActorPipeline.Actors.Downscaling
{
    internal unsafe class OpenClDownscalerActor : LoggingActor
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

            Receive<DownscalingMessages.SetDownscalingSizesMessage>(HandleSizesChange);
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
            LogMessage(message);
            PerformPartialUpdateAndResize(message);
        }

        private void HandleSizesChange(DownscalingMessages.SetDownscalingSizesMessage message)
        {
            LogMessage(message);
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
                inputRegion = message.FinalInputRegion;
                needFullResize = true;
            }

            if (needFullResize)
            {
                PerformFullResize();
            }
        }

        private void PerformFullResize()
        {
            CLImageSize inputSize = new CLImageSize {Width = (uint) inputBuffer.Width, Height = (uint) inputBuffer.Height};
            CLImageSize outputSize = new CLImageSize { Width = (uint)outputBuffer.Width, Height = (uint)outputBuffer.Height };
            clKernel.SetMemoryArgument(0, inputBuffer);
            clKernel.SetMemoryArgument(1, outputBuffer);
            clKernel.SetValueArgument(2, inputSize);
            clKernel.SetValueArgument(3, outputSize);
            clCommandQueue.Execute(clKernel, null, new long[] {outputBuffer.Width, outputBuffer.Height}, null, null);

            byte[] data = new byte[outputBuffer.Height * outputBuffer.RowPitch];
            fixed (byte* ptr = data)
            {
                clCommandQueue.ReadFromImage(outputBuffer, new IntPtr(ptr), true, null);
            }

            parentDownscaler.Tell(
                new DownscalingMessages.OutputUpdatedMessage(
                    new ImageRegion(0, 0, outputBuffer.Width, outputBuffer.Height), data)
                );
        }

        private void PerformPartialUpdateAndResize(PipelineFilterMessages.PartialUpdateMessage message)
        {
            var realUpdatedRegion = inputRegion.IntersectWith(message.Changed);
            if (realUpdatedRegion == ImageRegion.Invalid)
            {
                // nothing to update
                return;
            }
            CopyDataToInputBuffer(message, realUpdatedRegion);
            ImageRegion outputUpdatedRegion = CalculateOutputUpdatedRegion(realUpdatedRegion);
            PerformPartialResize(outputUpdatedRegion);
        }

        private ImageRegion CalculateOutputUpdatedRegion(ImageRegion inputUpdatedRegion)
        {
            double scale = (double) outputBuffer.Width / inputRegion.Width;
            int left = Fit((int) Math.Floor((inputUpdatedRegion.Left-inputRegion.Left) * scale), 0, outputBuffer.Width);
            int top = Fit((int) Math.Floor((inputUpdatedRegion.Top-inputRegion.Top) * scale), 0, outputBuffer.Height);
            int right = Fit((int) Math.Ceiling((inputUpdatedRegion.Right-inputRegion.Left) * scale), 0, outputBuffer.Width);
            int bottom = Fit((int) Math.Ceiling((inputUpdatedRegion.Bottom-inputRegion.Top) * scale), 0, outputBuffer.Height);
            return new ImageRegion(left, top, right - left, bottom - top);
        }

        private static int Fit(int x, int min, int max) => Math.Max(min, Math.Min(x, max));
        
        private void PerformPartialResize(ImageRegion outputUpdatedRegion)
        {
            CLImageSize inputSize = new CLImageSize {Width = (uint) inputBuffer.Width, Height = (uint) inputBuffer.Height};
            CLImageSize outputSize = new CLImageSize { Width = (uint)outputBuffer.Width, Height = (uint)outputBuffer.Height };
            clKernel.SetMemoryArgument(0, inputBuffer);
            clKernel.SetMemoryArgument(1, outputBuffer);
            clKernel.SetValueArgument(2, inputSize);
            clKernel.SetValueArgument(3, outputSize);

            clCommandQueue.Execute(clKernel, new long[]{outputUpdatedRegion.Left, outputUpdatedRegion.Top}, new long[] {outputUpdatedRegion.Width, outputUpdatedRegion.Height}, null, null);

            int bytesPerPixel = GetBytesPerPixel(currentFormat);
            byte[] data = new byte[outputUpdatedRegion.Width * outputUpdatedRegion.Height * bytesPerPixel];
            var outputBufferPtr = clCommandQueue.Map(outputBuffer, true, ComputeMemoryMappingFlags.Read,
                new SysIntX3(outputUpdatedRegion.Left, outputUpdatedRegion.Top, 0),
                new SysIntX3(outputUpdatedRegion.Width, outputUpdatedRegion.Height, 1),
                null);
            Marshal.Copy(outputBufferPtr, data, 0, data.Length);
            clCommandQueue.Unmap(outputBuffer, ref outputBufferPtr, null);

            parentDownscaler.Tell(new DownscalingMessages.OutputUpdatedMessage(outputUpdatedRegion, data));
        }

        private void CopyDataToInputBuffer(PipelineFilterMessages.PartialUpdateMessage message, ImageRegion realUpdatedRegion)
        {
            var inputBufferPtr = clCommandQueue.Map(inputBuffer, true, ComputeMemoryMappingFlags.Write,
                new SysIntX3(realUpdatedRegion.Left, realUpdatedRegion.Top, 0),
                new SysIntX3(realUpdatedRegion.Width, realUpdatedRegion.Height, 1),
                null);
            int bytesPerPixel = GetBytesPerPixel(currentFormat);
            for (int i = 0; i < realUpdatedRegion.Height; i++)
            {
                int y = i + realUpdatedRegion.Top - message.Changed.Top;
                int x = realUpdatedRegion.Left - message.Changed.Left;
                int startIndex = y * message.Changed.Width * bytesPerPixel + x * bytesPerPixel;
                int length = realUpdatedRegion.Width * bytesPerPixel;
                Marshal.Copy(message.Data, startIndex, inputBufferPtr+i*realUpdatedRegion.Width*bytesPerPixel, length);
            }
            clCommandQueue.Unmap(inputBuffer, ref inputBufferPtr, null);
        }

        private bool ZoomedRegionChanged(DownscalingMessages.SetDownscalingSizesMessage message)
        {
            return inputRegion != message.FinalInputRegion;
        }

        private bool OutputSizeChanged(DownscalingMessages.SetDownscalingSizesMessage message)
        {
            return outputBuffer == null ||
                   outputBuffer.Width != message.FinalOutputSize.Width ||
                   outputBuffer.Height != message.FinalOutputSize.Height;
        }

        private bool InputBufferFormatChanged(DownscalingMessages.SetDownscalingSizesMessage message)
        {
            return (inputBuffer == null ||
                    message.Format != currentFormat ||
                    message.InputSize.Width != inputBuffer.Width ||
                    message.InputSize.Height != inputBuffer.Height);
        }

        private void CreateNewInputBuffer(DownscalingMessages.SetDownscalingSizesMessage message)
        {
            inputBuffer?.Dispose();
#pragma warning disable CS0618
            inputBuffer = new ComputeImage2D(clContext, ComputeMemoryFlags.ReadOnly, GetBufferFormat(message.Format),
                message.InputSize.Width, message.InputSize.Height, 0, IntPtr.Zero);
#pragma warning restore CS0618
            currentFormat = message.Format;
        }

        private void CreateNewOutputBuffer(DownscalingMessages.SetDownscalingSizesMessage message)
        {
            outputBuffer?.Dispose();
#pragma warning disable CS0618
            outputBuffer = new ComputeImage2D(clContext, ComputeMemoryFlags.WriteOnly, GetBufferFormat(message.Format),
                message.FinalOutputSize.Width, message.FinalOutputSize.Height, 0, IntPtr.Zero);
#pragma warning restore CS0618
        }

        private static int GetBytesPerPixel(ImageFormat format)
        {
            switch (format)
            {
                case ImageFormat.Grayscale8: return 1;
                case ImageFormat.Grayscale16: return 2;
                case ImageFormat.Color24: return 3;
                default:
                    throw new NotSupportedException("Unsupported image format");
            }
        }

        private static ComputeImageFormat GetBufferFormat(ImageFormat format)
        {
            switch (format)
            {
                case ImageFormat.Grayscale8:
                    return new ComputeImageFormat(ComputeImageChannelOrder.Luminance,
                        ComputeImageChannelType.UNormInt8);
                case ImageFormat.Grayscale16:
                    return new ComputeImageFormat(ComputeImageChannelOrder.Luminance,
                        ComputeImageChannelType.UNormInt16);
                case ImageFormat.Color24:
                    return new ComputeImageFormat(ComputeImageChannelOrder.Rgba,
                        ComputeImageChannelType.UnsignedInt8);
                default:
                    throw new NotSupportedException("Unsupported image format");
            }
        }

        private static string GetProgramCode()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var streamReader = new StreamReader(assembly.GetManifestResourceStream("ActorPipeline.Actors.Downscaling.DownscalingKernels.cl")))
            {
                return streamReader.ReadToEnd();
            }
        }

        private struct CLImageSize
        {
            public uint Width;
            public uint Height;
        };
    }
}
