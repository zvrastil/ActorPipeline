
using ActorPipeline.Actors.Downscaling;
using ActorPipeline.Messages;
using Akka.Actor;

namespace ActorPipeline.Actors
{
    public sealed class ImageDownscalerActor : FilterActorBase
    {
        private PipelineFilterMessages.FormatChangedMessage inputFormat;
        private DisplayMessages.SetDisplaySettingsMessage outputSettings;
        private DownscalingSizes outputFormat;
        private readonly IActorRef downscaler;

        public ImageDownscalerActor() : base(FilterFlags.ProcessingFilter)
        {
            BaseMessages();
            Receive<PipelineFilterMessages.FormatChangedMessage>(HandleFormatChange);
            Receive<DisplayMessages.SetDisplaySettingsMessage>(HandleDisplaySettings);
            Receive<PipelineFilterMessages.PartialUpdateMessage>(HandlePartialUpdate);
            Receive<DownscalingMessages.SetDownscalingSizesMessage>(HandleFinalSizes);
            Receive<DownscalingMessages.OutputUpdatedMessage>(HandleOutputUpdated);
            downscaler = Context.ActorOf(Props.Create(() => new OpenClDownscalerActor(Self)), "downscaler");
        }

        private void HandleOutputUpdated(DownscalingMessages.OutputUpdatedMessage message)
        {
            LogMessage(message);
            TellToSubscribers(new PipelineFilterMessages.PartialUpdateMessage(message.OutputFormatId, message.Changed, message.Data));
        }

        private void HandleFinalSizes(DownscalingMessages.SetDownscalingSizesMessage message)
        {
            LogMessage(message);
            outputFormat = message.Sizes;
            TellToSubscribers(CreateOutputFormatMessage());
        }


        private void HandlePartialUpdate(PipelineFilterMessages.PartialUpdateMessage message)
        {
            if (message.FormatChangeId != inputFormat.Id)
            {
                return;
            }
            LogMessage(message);
            if (!IsReadyForWork())
            {
                return;
            }
            downscaler.Tell(message);
        }

        private void HandleFormatChange(PipelineFilterMessages.FormatChangedMessage message)
        {
            LogMessage(message);
            inputFormat = message;
            UpdateDownscalerSettings();
        }

        private void HandleDisplaySettings(DisplayMessages.SetDisplaySettingsMessage message)
        {
            LogMessage(message);
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

            downscaler.Tell(new DownscalingMessages.SetDownscalingSettingsMessage(
                inputFormat.Size,
                outputSettings.DisplaySize, 
                inputFormat.Format,
                outputSettings.RelativeRegion));
        }

        private PipelineFilterMessages.FormatChangedMessage CreateOutputFormatMessage()
        {
            return new PipelineFilterMessages.FormatChangedMessage(outputFormat.Id, outputFormat.FinalOutputSize, outputFormat.Format);
        }

        private bool IsReadyForWork()
        {
            return inputFormat != null && outputSettings != null;
        }
    }
}
