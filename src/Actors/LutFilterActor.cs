
using ActorPipeline.Messages;

namespace ActorPipeline.Actors
{
    public sealed class LutFilterActor : ProcessingFilterActorBase
    {
        public LutFilterActor() 
        {
            BaseMessages();
        }

        protected override void HandleFormatChange(PipelineFilterMessages.FormatChangedMessage message)
        {
            base.HandleFormatChange(message);
        }

        protected override void HandleImageUpdate(PipelineFilterMessages.PartialUpdateMessage message)
        {
            base.HandleImageUpdate(message);
        }
    }
}
