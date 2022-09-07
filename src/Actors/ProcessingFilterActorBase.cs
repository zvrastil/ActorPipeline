using ActorPipeline.Messages;
using Akka.Actor;

namespace ActorPipeline.Actors
{
    public abstract class ProcessingFilterActorBase : FilterActorBase
    {
        protected PipelineFilterMessages.FormatChangedMessage currentFormat;

        protected ProcessingFilterActorBase() : base(FilterFlags.ProcessingFilter)
        {
        }

        protected override void BaseMessages()
        {
            base.BaseMessages();
            Receive<PipelineFilterMessages.FormatChangedMessage>(HandleFormatChange);
            Receive<PipelineFilterMessages.PartialUpdateMessage>(HandleImageUpdate);
        }

        protected virtual void HandleFormatChange(PipelineFilterMessages.FormatChangedMessage message)
        {
            LogMessage(message);
            currentFormat = message;
            TellToSubscribers(message);
        }

        protected virtual void HandleImageUpdate(PipelineFilterMessages.PartialUpdateMessage message)
        {
            LogMessage(message);
            TellToSubscribers(message);
        }

        protected override void OnSubscribed(IActorRef actor)
        {
            base.OnSubscribed(actor);
            if (currentFormat != null)
            {
                actor.Tell(currentFormat);
            }
        }
    }
}
