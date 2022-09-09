using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using ActorPipeline.Images2;
using ActorPipeline.Messages;
using Akka.Actor;
using Cloo;

namespace ActorPipeline.Actors.Downscaling
{
    internal unsafe class OpenClDownscalerActor : LoggingActor
    {
        private readonly IActorRef parentDownscaler;
        private readonly ComputeContext clContext;
        private ComputeCommandQueue clCommandQueue;
        private ComputeProgram clProgram;
        private ComputeKernel clClearKernel;
        private ComputeKernel clResizeKernel;
        private ComputeImage2D inputBuffer;
        private ComputeImage2D outputBuffer;
        private ImageFormat currentFormat;
        private FractionalImageRegion inputRegion;
        private long currentFormatId;

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

            clClearKernel = clProgram.CreateKernel("clear");
            clResizeKernel = clProgram.CreateKernel("resize");

            Receive<DownscalingMessages.SetDownscalingSizesMessage>(HandleSizesChange);
            Receive<PipelineFilterMessages.PartialUpdateMessage>(HandlePartialUpdate);
        }

        protected override void PostStop()
        {
            inputBuffer?.Dispose();
            inputBuffer = null;
            outputBuffer?.Dispose();
            outputBuffer = null;
            clResizeKernel?.Dispose();
            clResizeKernel = null;
            clClearKernel?.Dispose();
            clClearKernel = null;
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

            if (OutputSizeOrFormatChanged(message))
            {
                CreateNewOutputBuffer(message);
                needFullResize = true;
            }

            currentFormatId = message.Id;
            currentFormat = message.Format;

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
            var sw = Stopwatch.StartNew();
            CLImageRegion inRegion = new CLImageRegion 
            {
                Left = (float) inputRegion.Left, Top = (float) inputRegion.Top, Width = (float) inputRegion.Width, Height = (float) inputRegion.Height
            };
            CLImageSize outputSize = new CLImageSize
            {
                Width = (uint)outputBuffer.Width, Height = (uint)outputBuffer.Height
            };
            clResizeKernel.SetMemoryArgument(0, inputBuffer);
            clResizeKernel.SetMemoryArgument(1, outputBuffer);
            clResizeKernel.SetValueArgument(2, inRegion);
            clResizeKernel.SetValueArgument(3, outputSize);

            var events = new List<ComputeEventBase>();
            clCommandQueue.Execute(clResizeKernel, null, new long[] {outputBuffer.Width, outputBuffer.Height}, null, events);

            byte[] data = new byte[outputBuffer.Height * outputBuffer.RowPitch];
            fixed (byte* ptr = data)
            {
                clCommandQueue.ReadFromImage(outputBuffer, new IntPtr(ptr), true, events);
            }
            logger.Debug($"  OpenCL FullResize {inRegion.Width:F1}x{inRegion.Height:F1} -> {outputSize.Width}x{outputSize.Height} finished in {sw.ElapsedMilliseconds} ms");

            events.ForEach(e => e.Dispose());

            parentDownscaler.Tell(
                new DownscalingMessages.OutputUpdatedMessage(currentFormatId,
                    new ImageRegion(0, 0, outputBuffer.Width, outputBuffer.Height), data)
                );
        }

        private void PerformPartialUpdateAndResize(PipelineFilterMessages.PartialUpdateMessage message)
        {
            var unmapping = CopyDataToInputBuffer(message);
            
            var realUpdatedRegion = inputRegion.IntersectWith(message.Changed);
            if (realUpdatedRegion == FractionalImageRegion.Invalid)
            {
                unmapping.Dispose();
                // nothing to resize
                return;
            }
            
            ImageRegion outputUpdatedRegion = CalculateOutputUpdatedRegion(realUpdatedRegion);

            var outputUpdatedMessage = PerformPartialResize(outputUpdatedRegion, unmapping);
            parentDownscaler.Tell(outputUpdatedMessage);
        }

        private ImageRegion CalculateOutputUpdatedRegion(FractionalImageRegion inputUpdatedRegion)
        {
            double scale = outputBuffer.Width / inputRegion.Width;
            int left = Fit((int) Math.Floor((inputUpdatedRegion.Left-inputRegion.Left) * scale), 0, outputBuffer.Width);
            int top = Fit((int) Math.Floor((inputUpdatedRegion.Top-inputRegion.Top) * scale), 0, outputBuffer.Height);
            int right = Fit((int) Math.Ceiling((inputUpdatedRegion.Right-inputRegion.Left) * scale), 1, outputBuffer.Width);
            int bottom = Fit((int) Math.Ceiling((inputUpdatedRegion.Bottom-inputRegion.Top) * scale), 1, outputBuffer.Height);
            return new ImageRegion(left, top, right - left, bottom - top);
        }

        private static int Fit(int x, int min, int max) => Math.Max(min, Math.Min(x, max));
        
        private DownscalingMessages.OutputUpdatedMessage PerformPartialResize(ImageRegion outputUpdatedRegion, ComputeEventBase unmapping)
        {
            var sw = Stopwatch.StartNew();
            CLImageRegion inRegion = new CLImageRegion
            {
                Left = (float)inputRegion.Left,
                Top = (float)inputRegion.Top,
                Width = (float)inputRegion.Width,
                Height = (float)inputRegion.Height
            };
            CLImageSize outputSize = new CLImageSize { Width = (uint)outputBuffer.Width, Height = (uint)outputBuffer.Height };
            clResizeKernel.SetMemoryArgument(0, inputBuffer);
            clResizeKernel.SetMemoryArgument(1, outputBuffer);
            clResizeKernel.SetValueArgument(2, inRegion);
            clResizeKernel.SetValueArgument(3, outputSize);

            var events = new List<ComputeEventBase> {unmapping};
            clCommandQueue.Execute(clResizeKernel, new long[]{outputUpdatedRegion.Left, outputUpdatedRegion.Top}, new long[] {outputUpdatedRegion.Width, outputUpdatedRegion.Height}, null, 
                events);

            int bytesPerPixel = inputBuffer.ElementSize;
            byte[] data = new byte[outputUpdatedRegion.Width * outputUpdatedRegion.Height * bytesPerPixel];
            var outputBufferPtr = clCommandQueue.Map(outputBuffer, true, ComputeMemoryMappingFlags.Read,
                new SysIntX3(outputUpdatedRegion.Left, outputUpdatedRegion.Top, 0),
                new SysIntX3(outputUpdatedRegion.Width, outputUpdatedRegion.Height, 1),
                events);
            Marshal.Copy(outputBufferPtr, data, 0, data.Length);
            clCommandQueue.Unmap(outputBuffer, ref outputBufferPtr, null);
            double scale = (double) inRegion.Height / outputSize.Height;

            events.ForEach(e => e.Dispose());
            logger.Debug($"  OpenCL PartialResize {outputUpdatedRegion.Width*scale:F1}x{outputUpdatedRegion.Height*scale:F1} -> {outputUpdatedRegion.Width}x{outputUpdatedRegion.Height} finished in {sw.ElapsedMilliseconds} ms");

            return new DownscalingMessages.OutputUpdatedMessage(currentFormatId, outputUpdatedRegion, data);
        }

        private ComputeEventBase CopyDataToInputBuffer(PipelineFilterMessages.PartialUpdateMessage message)
        {
            var inputBufferPtr = clCommandQueue.Map(inputBuffer, true, ComputeMemoryMappingFlags.Write,
                new SysIntX3(message.Changed.Left, message.Changed.Top, 0),
                new SysIntX3(message.Changed.Width, message.Changed.Height, 1),
                null);
            int bytesPerPixel = inputBuffer.ElementSize;
            for (int i = 0; i < message.Changed.Height; i++)
            {
                int y = i + message.Changed.Top - message.Changed.Top;
                int x = message.Changed.Left - message.Changed.Left;
                int startIndex = y * message.Changed.Width * bytesPerPixel + x * bytesPerPixel;
                int length = message.Changed.Width * bytesPerPixel;
                Marshal.Copy(message.Data, startIndex, inputBufferPtr+i* message.Changed.Width*bytesPerPixel, length);
            }

            var events = new List<ComputeEventBase>();
            clCommandQueue.Unmap(inputBuffer, ref inputBufferPtr, events);
            return events[0];
        }

        private bool ZoomedRegionChanged(DownscalingMessages.SetDownscalingSizesMessage message)
        {
            return inputRegion != message.FinalInputRegion;
        }

        private bool OutputSizeOrFormatChanged(DownscalingMessages.SetDownscalingSizesMessage message)
        {
            return outputBuffer == null ||
                   message.Format != currentFormat ||
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
            inputBuffer = new ComputeImage2D(clContext, ComputeMemoryFlags.ReadWrite, GetBufferFormat(message.Format),
                message.InputSize.Width, message.InputSize.Height, 0, IntPtr.Zero);
#pragma warning restore CS0618
            ClearBuffer(inputBuffer);
        }

        private void CreateNewOutputBuffer(DownscalingMessages.SetDownscalingSizesMessage message)
        {
            outputBuffer?.Dispose();
#pragma warning disable CS0618
            outputBuffer = new ComputeImage2D(clContext, ComputeMemoryFlags.WriteOnly, GetBufferFormat(message.Format),
                message.FinalOutputSize.Width, message.FinalOutputSize.Height, 0, IntPtr.Zero);
#pragma warning restore CS0618
            ClearBuffer(outputBuffer);
        }

        private void ClearBuffer(ComputeImage2D buffer)
        {
            clClearKernel.SetMemoryArgument(0, buffer);
            clCommandQueue.Execute(clClearKernel, null, new long[] { buffer.Width, buffer.Height }, null, null);
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
                case ImageFormat.Bgra32:
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
        }

        private struct CLImageRegion
        {
            public float Left;
            public float Top;
            public float Width;
            public float Height;
        }
    }
}
