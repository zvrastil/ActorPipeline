using ActorPipeline.Images;
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
        }

        public sealed class SubscribeMessage { }

        public sealed class FormatChangedMessage
        {
            public FormatChangedMessage(int width, int height, ImageFormat format)
            {
                Width = width;
                Height = height;
                Format = format;
            }

            public FormatChangedMessage(IImage image)
            {
                Width = image.Width;
                Height = image.Height;
                Format = image.Format;
            }

            public int Width { get; }
            public int Height { get; }
            public ImageFormat Format { get; }
        }

        public sealed class PartialUpdateMessage
        {
            public PartialUpdateMessage(IntRectangle changed, byte[] data)
            {
                Changed = changed;
                Data = data;
            }

            public IntRectangle Changed { get; }
            public byte[] Data { get; }
        }
                   
    }
}
