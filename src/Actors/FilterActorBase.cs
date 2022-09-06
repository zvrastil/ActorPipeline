using ActorPipeline.Messages;
using Akka.Actor;
using System;
using System.Collections.Generic;

namespace ActorPipeline.Actors
{
    [Flags]
    public enum FilterFlags
    {
        SourceFilter = 0x1,
        TargetFilter = 0x2,
        ProcessingFilter = SourceFilter | TargetFilter,
    }

    public abstract class FilterActorBase : ReceiveActor
    {
        private readonly HashSet<IActorRef> subscribers = new HashSet<IActorRef>();
        private readonly FilterFlags filterFlags;

        protected FilterActorBase(FilterFlags filterFlags)
        {
            this.filterFlags = filterFlags;
        }

        protected virtual void BaseMessages()
        {
            if (filterFlags.HasFlag(FilterFlags.SourceFilter))
            {
                Receive<PipelineFilterMessages.SubscribeMessage>(HandleSubscribe);
            }
            if (filterFlags.HasFlag(FilterFlags.TargetFilter))
            {
                Receive<PipelineFilterMessages.ConnectToMessage>(HandleConnectTo);
            }
        }        

        protected void TellToSubscribers<T>(T message)
        {
            if (!filterFlags.HasFlag(FilterFlags.SourceFilter))
            {
                throw new InvalidOperationException("Only source filter can have subscribers.");
            }
            foreach (var subscriber in subscribers)
            {
                subscriber.Tell(message);
            }
        }

        protected virtual void OnSubscribed(IActorRef actor)
        {
        }

        private void HandleSubscribe(PipelineFilterMessages.SubscribeMessage message)
        {
            subscribers.Add(message.Subscriber);
            OnSubscribed(message.Subscriber);   
        }

        private void HandleConnectTo(PipelineFilterMessages.ConnectToMessage message)
        {
            foreach (var sourceFilter in message.SourceFilters)
            {
                sourceFilter.Tell(new PipelineFilterMessages.SubscribeMessage(Self));
            }
        }

    }
}
