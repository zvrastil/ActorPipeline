
using ActorPipeline.Actors.Downscaling;
using ActorPipeline.Messages;
using Akka.Actor;

namespace ActorPipeline.Actors
{
    public sealed class ImageDownscalerActor : FilterActorBase
    {
        private PipelineFilterMessages.FormatChangedMessage inputFormat;
        private DisplayMessages.SetDisplaySettingsMessage outputSettings;
        private readonly IActorRef downscalerCoordinator;

        public ImageDownscalerActor() : base(FilterFlags.ProcessingFilter)
        {
            BaseMessages();
            Receive<PipelineFilterMessages.FormatChangedMessage>(HandleFormatChange);
            Receive<DisplayMessages.SetDisplaySettingsMessage>(HandleDisplaySettings);
            Receive<PipelineFilterMessages.PartialUpdateMessage>(HandlePartialUpdate);
            downscalerCoordinator = Context.ActorOf(Props.Create(() => new DownscalerCoordinatorActor(Self)));
        }

        
        private void HandlePartialUpdate(PipelineFilterMessages.PartialUpdateMessage message)
        {
            if (!IsReadyForWork())
            {
                return;
            }
            downscalerCoordinator.Tell(message);
        }

        private void HandleFormatChange(PipelineFilterMessages.FormatChangedMessage message)
        {
            inputFormat = message;
            UpdateFormatIfReady();
        }

        private void HandleDisplaySettings(DisplayMessages.SetDisplaySettingsMessage message)
        {
            outputSettings = message;
            UpdateFormatIfReady();
        }

        protected override void OnSubscribed(IActorRef actor)
        {
            base.OnSubscribed(actor);
            if (inputFormat != null && outputSettings != null)
            {
                actor.Tell(CreateOutputFormatMessage());
            }
        }

        private void UpdateFormatIfReady()
        {
            if (!IsReadyForWork())
            {
                return;
            }

            downscalerCoordinator.Tell(new DownscalingMessages.SetDownscalingSettingsMessage(
                inputFormat.Width, inputFormat.Height,
                outputSettings.DisplaySize.Width, outputSettings.DisplaySize.Height, 
                inputFormat.Format,
                outputSettings.RelativeRegion));
            TellToSubscribers(CreateOutputFormatMessage());
        }

        private PipelineFilterMessages.FormatChangedMessage CreateOutputFormatMessage()
        {
            return new PipelineFilterMessages.FormatChangedMessage(outputSettings.DisplaySize.Width, outputSettings.DisplaySize.Height, inputFormat.Format);
        }

        private bool IsReadyForWork()
        {
            return inputFormat != null && outputSettings != null;
        }
    }
}
