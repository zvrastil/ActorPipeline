using ActorPipeline.Images2;
using ActorPipeline.Messages.Data;
using Akka.Actor;

namespace ActorPipeline.Messages
{
    public class PipelineControlMessages
    {
        public sealed class StartMessage
        {
            public override string ToString() => "PipelineControl_Start";
        }

        public sealed class StopMessage
        {
            public override string ToString() => "PipelineControl_Stop";
        }

        public sealed class CreateFilterMessage
        {
            public CreateFilterMessage(Props props, FilterProperties filterProperties)
            {
                Props = props;
                FilterProperties = filterProperties;
            }

            public Props Props { get; }
            public FilterProperties FilterProperties { get; }

            public override string ToString() =>
                $"PipelineControl_CreateFilter({FilterProperties.Name}, sourceFilter: {FilterProperties.IsSourceFilter})";
        }

        public sealed class SetImageFormatMessage
        {
            public SetImageFormatMessage(ImageSize size, ImageFormat format)
            {
                Size = size;
                Format = format;
            }

            public ImageSize Size { get; }
            public ImageFormat Format { get; }
            
            public override string ToString() => $"PipelineControl_SetImageFormat({Size}, {Format})";
        }

        public sealed class SetDwellTimeMessage
        {
            public SetDwellTimeMessage(long dwellTimeNanoseconds)
            {
                DwellTimeNanoseconds = dwellTimeNanoseconds;
            }

            public long DwellTimeNanoseconds { get; }

            public override string ToString() => $"PipelineControl_SetDwellTime({DwellTimeNanoseconds}ns)";
        }
    }
}
