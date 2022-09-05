using ActorPipeline.Images;
using ActorPipeline.Messages.Data;
using Akka.Actor;
using System;

namespace ActorPipeline.Messages
{
    public static class PipelineControlMessages
    {
        public sealed class StartMessage { }
        public sealed class StopMessage { }

        public sealed class CreateFilterMessage
        {
            public CreateFilterMessage(Props props, FilterProperties filterProperties)
            {
                Props = props;
                FilterProperties = filterProperties;
            }

            public Props Props { get; }
            public FilterProperties FilterProperties { get; }
        }

        public sealed class SetImageFormatMessage
        {
            public SetImageFormatMessage(int width, int height, ImageFormat format)
            {
                Width = width;
                Height = height;
                Format = format;
            }

            public int Width { get; }
            public int Height { get; }
            public ImageFormat Format { get; }
        }

        public sealed class SetDwellTimeMessage
        {
            public SetDwellTimeMessage(int dwellTimeNanoseconds)
            {
                DwellTimeNanoseconds = dwellTimeNanoseconds;
            }

            public int DwellTimeNanoseconds { get; }
        }
    }
}
