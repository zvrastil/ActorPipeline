using ActorPipeline.Images2;
using ActorPipeline.Messages.Data;
using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ActorPipeline.Messages
{
    public class PipelineFilterMessages
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

        public sealed class DisconnectFromMessage
        {

            public DisconnectFromMessage(IActorRef sourceFilter, params IActorRef[] otherSourceFilters)
            {
                if (sourceFilter == null)
                {
                    throw new ArgumentNullException(nameof(sourceFilter));
                }

                SourceFilters = new[] { sourceFilter }.Concat(otherSourceFilters).ToList();
            }

            public IReadOnlyList<IActorRef> SourceFilters { get; }

            public override string ToString() => $"PipelineFilter_DisconnectFrom({string.Join(", ", SourceFilters.Select(sf => sf.Path.Name))})";

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
        
        public sealed class SendZoomedImageMessage
        {
            public SendZoomedImageMessage(bool zoomedImage, ZoomedImageArea area)
            {
                ZoomedImage = zoomedImage;
                Area = area;
            }

            public bool ZoomedImage { get; }
            public ZoomedImageArea Area { get; }

            public override string ToString() => $"PipelineFilter.SendZoomedImage(zoomedArea: {ZoomedImage}, {Area})";
        }

        public sealed class SendImageDataMessage
        {
            public SendImageDataMessage(long requestId, ImageRegion region)
            {
                RequestId = requestId;
                Region = region;
            }

            public long RequestId { get; }
            public ImageRegion Region { get; }

            public override string ToString() => $"PipelineFilter.SendImageData({RequestId}, {Region})";
        }

        public sealed class ImageDataMessage
        {
            public ImageDataMessage(long requestId, ImageFormat format, ImageSize imageSize, int firstLine, int numberOfLines, byte[] data)
            {
                RequestId = requestId;
                Format = format;
                ImageSize = imageSize;
                FirstLine = firstLine;
                NumberOfLines = numberOfLines;
                Data = data;
            }

            public ImageDataMessage WithNewData(byte[] data)
            {
                return new ImageDataMessage(RequestId, Format, ImageSize, FirstLine, NumberOfLines, data);
            }

            public ImageDataMessage WithNewDataAndFormat(ImageFormat format, byte[] data)
            {
                return new ImageDataMessage(RequestId, format, ImageSize, FirstLine, NumberOfLines, data);
            }

            public long RequestId { get; }

            public ImageFormat Format { get; }

            public ImageSize ImageSize { get; }
            public int FirstLine { get; }
            public int NumberOfLines { get; }        
            public byte[] Data { get; }

            public override string ToString() => $"PipelineFilter.ImageData({RequestId}, {Format}, {ImageSize}, {FirstLine}, {NumberOfLines}, {Data.Length})";
        }

    }
}
