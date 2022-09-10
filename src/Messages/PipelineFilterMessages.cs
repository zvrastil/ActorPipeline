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

        public sealed class UnsubscribeMessage
        {
            public UnsubscribeMessage(IActorRef subscriber)
            {
                Subscriber = subscriber;
            }

            public IActorRef Subscriber { get; }

            public override string ToString() => $"PipelineFilter_Unsubscribe({Subscriber.Path.Name})";
        }

        public sealed class FormatChangedMessage
        {
            public FormatChangedMessage(long id, ImageSize size, ImageFormat format)
            {
                Id = id;
                Size = size;
                Format = format;
            }

            public FormatChangedMessage(long id, IImage image) : this(id, image.Size, image.Format)
            {
            }

            public long Id { get; }
            public ImageSize Size { get; }
            public ImageFormat Format { get; }
            public override string ToString() => $"PipelineFilter_FormatChanged({Id}, {Size}, {Format})";
        }

        public sealed class PartialUpdateMessage
        {
            public PartialUpdateMessage(long formatChangeId, ImageRegion changed, byte[] data)
            {
                FormatChangeId = formatChangeId;
                Changed = changed;
                Data = data;
            }

            public long FormatChangeId { get; }
            public ImageRegion Changed { get; }
            public byte[] Data { get; }

            public override string ToString() => $"PipelineFilter_PartialUpdate({FormatChangeId}, {Changed}, byte[{Data.Length}])";
        }
                   
    }
}
