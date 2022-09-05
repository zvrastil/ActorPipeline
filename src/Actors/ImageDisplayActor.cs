using ActorPipeline.Messages;
using System;

namespace ActorPipeline.Actors
{
    public class ImageDisplayActor : FilterActorBase
    {
        public ImageDisplayActor() : base(FilterFlags.ProcessingFilter)
        {
            BaseMessages();
            Receive<PipelineFilterMessages.FormatChangedMessage>(HandleFormatChange);
            Receive<PipelineFilterMessages.PartialUpdateMessage>(HandleImageUpdate);
        }

        private void HandleFormatChange(PipelineFilterMessages.FormatChangedMessage message)
        {
            System.Diagnostics.Trace.WriteLine($"ImageDisplayActor.FormatChanged to {message.Width}x{message.Height}x{message.Format}");
            TellToSubscribers(message);
        }

        private void HandleImageUpdate(PipelineFilterMessages.PartialUpdateMessage message)
        {
            System.Diagnostics.Trace.WriteLine($"ImageDisplayActor.ImageUpdate  {message.Changed}");
            TellToSubscribers(message);
        }
    }
}
