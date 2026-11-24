using ActorPipeline.Images2;
using Akka.Actor;
using Akka.Event;
using Cloo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ActorPipeline.Actors.Downscaling
{
    internal unsafe class OpenClRunner : IDisposable
    {
        private readonly ILoggingAdapter logger;

        private readonly ComputeContext clContext;
        private readonly ComputeCommandQueue clCommandQueue;
        private readonly ComputeProgram clProgram;
        private readonly ComputeKernel clClearKernel;
        private readonly ComputeKernel clResizeKernel;
        private ComputeImage2D inputBuffer;
        private Dictionary<IActorRef, ComputeImage2D> outputBuffers = new Dictionary<IActorRef, ComputeImage2D>();
        

        public OpenClRunner(ILoggingAdapter logger)
        {
            var platform = ComputePlatform.Platforms.First();
            var device = platform.Devices.First(d => d.Type == ComputeDeviceTypes.Gpu);
            clContext = new ComputeContext(new[] { device }, new ComputeContextPropertyList(platform), null, IntPtr.Zero);

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
            this.logger = logger;
        }

        public void CreateInputBuffer(ImageFormat format, int width, int height)
        {
            inputBuffer?.Dispose();
            inputBuffer = CreateBuffer(format, width, height, ComputeMemoryFlags.ReadWrite);
        }


        public void CreateOutputBuffer(IActorRef forActor, ImageFormat format, int width, int height)
        {
            if (outputBuffers.ContainsKey(forActor))
            {
                outputBuffers[forActor].Dispose();
            }
            outputBuffers[forActor] = CreateBuffer(format, width, height, ComputeMemoryFlags.WriteOnly);
        }

        public void FreeOutputBuffer(IActorRef forActor)
        {
            if (outputBuffers.ContainsKey(forActor))
            {
                outputBuffers[forActor].Dispose();
                outputBuffers.Remove(forActor);
            }
        }

        public byte[] FullResize(IActorRef forActor, FractionalImageRegion inputRegion)
        {
            lock (this)
            {
                var sw = Stopwatch.StartNew();
                var outputBuffer = outputBuffers[forActor];
                CLImageRegion inRegion = new CLImageRegion
                {
                    Left = (float)inputRegion.Left,
                    Top = (float)inputRegion.Top,
                    Width = (float)inputRegion.Width,
                    Height = (float)inputRegion.Height
                };
                CLImageSize outputSize = new CLImageSize
                {
                    Width = (uint)outputBuffer.Width,
                    Height = (uint)outputBuffer.Height
                };
                clResizeKernel.SetMemoryArgument(0, inputBuffer);
                clResizeKernel.SetMemoryArgument(1, outputBuffer);
                clResizeKernel.SetValueArgument(2, inRegion);
                clResizeKernel.SetValueArgument(3, outputSize);

                var events = new List<ComputeEventBase>();
                clCommandQueue.Execute(clResizeKernel, null, new long[] { outputBuffer.Width, outputBuffer.Height }, null, events);

                byte[] data = new byte[outputBuffer.Height * outputBuffer.RowPitch];
                fixed (byte* ptr = data)
                {
                    clCommandQueue.ReadFromImage(outputBuffer, new IntPtr(ptr), true, events);
                }

                events.ForEach(e => e.Dispose());
                logger.Debug($"  OpenCL FullResize {inRegion.Width:F1}x{inRegion.Height:F1} -> {outputSize.Width}x{outputSize.Height} finished in {sw.ElapsedMilliseconds} ms");

                return data;
            }
        }

        public byte[] PartialResize(IActorRef forActor, FractionalImageRegion inputRegion, ImageRegion outputRegion)
        {
            lock (this)
            {
                var sw = Stopwatch.StartNew();
                var outputBuffer = outputBuffers[forActor];
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

                var events = new List<ComputeEventBase>();
                clCommandQueue.Execute(clResizeKernel, new long[] { outputRegion.Left, outputRegion.Top }, new long[] { outputRegion.Width, outputRegion.Height }, null,
                    events);

                int bytesPerPixel = inputBuffer.ElementSize;
                byte[] data = new byte[outputRegion.Width * outputRegion.Height * bytesPerPixel];
                var outputBufferPtr = clCommandQueue.Map(outputBuffer, true, ComputeMemoryMappingFlags.Read,
                    new SysIntX3(outputRegion.Left, outputRegion.Top, 0),
                    new SysIntX3(outputRegion.Width, outputRegion.Height, 1),
                    events);
                Marshal.Copy(outputBufferPtr, data, 0, data.Length);
                clCommandQueue.Unmap(outputBuffer, ref outputBufferPtr, null);
                double scale = (double)inRegion.Height / outputSize.Height;

                clCommandQueue.Flush();
                events.ForEach(e => e.Dispose());
                logger.Debug($"  OpenCL PartialResize {outputRegion.Width * scale:F1}x{outputRegion.Height * scale:F1} -> {outputRegion.Width}x{outputRegion.Height} finished in {sw.ElapsedMilliseconds} ms");

                return data;
            }
        }

        public void CopyToInputBuffer(ImageRegion region, byte[] data)
        {
            var inputBufferPtr = clCommandQueue.Map(inputBuffer, true, ComputeMemoryMappingFlags.Write,
                new SysIntX3(region.Left, region.Top, 0),
                new SysIntX3(region.Width, region.Height, 1),
                null);
            int bytesPerPixel = inputBuffer.ElementSize;
            for (int i = 0; i < region.Height; i++)
            {
                int y = i + region.Top - region.Top;
                int x = region.Left - region.Left;
                int startIndex = y * region.Width * bytesPerPixel + x * bytesPerPixel;
                int length = region.Width * bytesPerPixel;
                Marshal.Copy(data, startIndex, inputBufferPtr + i * region.Width * bytesPerPixel, length);
            }

            clCommandQueue.Unmap(inputBuffer, ref inputBufferPtr, null);
            clCommandQueue.Flush();
        }

        private ComputeImage2D CreateBuffer(ImageFormat format, int width, int height, ComputeMemoryFlags access)
        {
#pragma warning disable CS0618
            var buffer = new ComputeImage2D(clContext, access, GetBufferFormat(format), width, height, 0, IntPtr.Zero);
#pragma warning restore CS0618
            ClearBuffer(buffer);
            return buffer;
        }

        private void ClearBuffer(ComputeImage2D buffer)
        {
            clClearKernel.SetMemoryArgument(0, buffer);
            clCommandQueue.Execute(clClearKernel, null, new long[] { buffer.Width, buffer.Height }, null, null);
        }

        public void Dispose()
        {
            inputBuffer?.Dispose();
            foreach (var outputBuffer in outputBuffers.Values)
            {
                outputBuffer.Dispose();
            }
            clResizeKernel?.Dispose();
            clClearKernel?.Dispose();
            clProgram?.Dispose();
            clCommandQueue?.Dispose();
            clContext?.Dispose();
        }

        private static ComputeImageFormat GetBufferFormat(ImageFormat format)
        {
            switch (format)
            {
                case ImageFormat.Grayscale8:
                    return new ComputeImageFormat(ComputeImageChannelOrder.R,
                        ComputeImageChannelType.UNormInt8);
                case ImageFormat.Grayscale16:
                    return new ComputeImageFormat(ComputeImageChannelOrder.R,
                        ComputeImageChannelType.UNormInt16);
                case ImageFormat.Bgra32:
                    return new ComputeImageFormat(ComputeImageChannelOrder.Bgra,
                        ComputeImageChannelType.UNormInt8);
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
