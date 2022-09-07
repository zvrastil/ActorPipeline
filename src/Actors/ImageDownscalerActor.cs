
using ActorPipeline.Actors.Downscaling;
using ActorPipeline.Messages;
using Akka.Actor;

namespace ActorPipeline.Actors
{
    public sealed class ImageDownscalerActor : FilterActorBase
    {
        private PipelineFilterMessages.FormatChangedMessage inputFormat;
        private DisplayMessages.SetDisplaySettingsMessage outputSettings;
        private DownscalingMessages.SetDownscalingSizesMessage outputFormat;
        private readonly IActorRef downscalerCoordinator;

        public ImageDownscalerActor() : base(FilterFlags.ProcessingFilter)
        {
            BaseMessages();
            Receive<PipelineFilterMessages.FormatChangedMessage>(HandleFormatChange);
            Receive<DisplayMessages.SetDisplaySettingsMessage>(HandleDisplaySettings);
            Receive<PipelineFilterMessages.PartialUpdateMessage>(HandlePartialUpdate);
            Receive<DownscalingMessages.SetDownscalingSizesMessage>(HandleFinalSizes);
            Receive<DownscalingMessages.OutputUpdatedMessage>(HandleOutputUpdated);
            downscalerCoordinator = Context.ActorOf(Props.Create(() => new DownscalerCoordinatorActor(Self)));
        }

        private void HandleOutputUpdated(DownscalingMessages.OutputUpdatedMessage message)
        {
            TellToSubscribers(new PipelineFilterMessages.PartialUpdateMessage(message.Changed, message.Data));
        }

        private void HandleFinalSizes(DownscalingMessages.SetDownscalingSizesMessage message)
        {
            outputFormat = message;
            TellToSubscribers(CreateOutputFormatMessage());
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
            UpdateDownscalerSettings();
        }

        private void HandleDisplaySettings(DisplayMessages.SetDisplaySettingsMessage message)
        {
            outputSettings = message;
            UpdateDownscalerSettings();
        }

        protected override void OnSubscribed(IActorRef actor)
        {
            base.OnSubscribed(actor);
            if (outputFormat != null)
            {
                actor.Tell(CreateOutputFormatMessage());
            }
        }

        private void UpdateDownscalerSettings()
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
        }

        private PipelineFilterMessages.FormatChangedMessage CreateOutputFormatMessage()
        {
            return new PipelineFilterMessages.FormatChangedMessage(outputFormat.FinalOutputSize.Width, outputFormat.FinalOutputSize.Height, outputFormat.Format);
        }

        private bool IsReadyForWork()
        {
            return inputFormat != null && outputSettings != null;
        }
    }
}
