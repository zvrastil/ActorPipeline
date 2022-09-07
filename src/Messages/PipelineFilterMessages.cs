using ActorPipeline.Images2;
using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ActorPipeline.Messages
{
    public static class PipelineFilterMessages
    {
        public sealed class ConnectToMessage
        {

            public ConnectToMessage(IActorRef sourceFilter, params IActorRef[] otherSourceFilters)
            {
                if (sourceFilter == null)
                {
                    throw new ArgumentNullException(nameof(sourceFilter));
                }

                SourceFilters = new[] { sourceFilter }.Concat(otherSourceFilters).ToList();
            }

            public IReadOnlyList<IActorRef> SourceFilters { get; }

            public override string ToString() => $"PipelineFilter_ConnectTo({string.Join(", ", SourceFilters.Select(sf => sf.Path.Name))})";

        }

        public sealed class SubscribeMessage
        {
            public SubscribeMessage(IActorRef subscriber)
            {
                Subscriber = subscriber;
            }

            public IActorRef Subscriber { get; }

            public override string ToString() => $"PipelineFilter_Subscribe({Subscriber.Path.Name})";
        }

        public sealed class FormatChangedMessage
        {
            public FormatChangedMessage(ImageSize size, ImageFormat format)
            {
                Size = size;
                Format = format;
            }

            public FormatChangedMessage(IImage image)
            {
                Size = image.Size;
                Format = image.Format;
            }

            public ImageSize Size { get; }
            public ImageFormat Format { get; }
            public override string ToString() => $"PipelineFilter_FormatChanged({Size}, {Format})";
        }

        public sealed class PartialUpdateMessage
        {
            public PartialUpdateMessage(ImageRegion changed, byte[] data)
            {
                Changed = changed;
                Data = data;
            }

            public ImageRegion Changed { get; }
            public byte[] Data { get; }

            public override string ToString() => $"PipelineFilter_PartialUpdate({Changed}, byte[{Data.Length}])";
        }
                   
    }
}
