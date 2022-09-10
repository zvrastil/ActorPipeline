
using ActorPipeline.Actors.Downscaling;
using ActorPipeline.Messages;
using Akka.Actor;
using System.Collections.Generic;
using System.Linq;

namespace ActorPipeline.Actors
{
    public sealed class ImageDownscalerActor : FilterActorBase
    {
        private PipelineFilterMessages.FormatChangedMessage inputFormat;
        private Dictionary<IActorRef, DisplayMessages.SetDisplaySettingsMessage> outputSettings = new Dictionary<IActorRef, DisplayMessages.SetDisplaySettingsMessage>();
        private Dictionary<IActorRef, DownscalingSizes> outputFormats = new Dictionary<IActorRef, DownscalingSizes>();
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
            message.ForActor.Tell(new PipelineFilterMessages.PartialUpdateMessage(message.OutputFormatId, message.Changed, message.Data));
        }

        private void HandleFinalSizes(DownscalingMessages.SetDownscalingSizesMessage message)
        {
            LogMessage(message);
            outputFormats[message.ForActor] = message.Sizes;
            message.ForActor.Tell(CreateOutputFormatMessage(outputFormats[message.ForActor]));
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
            UpdateDownscalerSettings(outputSettings.Keys.ToArray());
        }

        private void HandleDisplaySettings(DisplayMessages.SetDisplaySettingsMessage message)
        {
            LogMessage(message);
            outputSettings[Sender] = message;
            UpdateDownscalerSettings(Sender);
        }

        protected override void OnSubscribed(IActorRef actor)
        {
            base.OnSubscribed(actor);
            outputSettings[Sender] = null;
        }

        private void UpdateDownscalerSettings(params IActorRef[] clients)
        {
            if (!IsReadyForWork())
            {
                return;
            }

            foreach (var client in clients)
            {
                if (outputSettings[client] != null)
                {
                    downscaler.Tell(new DownscalingMessages.SetDownscalingSettingsMessage(
                        client,
                        inputFormat.Size,
                        outputSettings[client].DisplaySize,
                        inputFormat.Format,
                        outputSettings[client].RelativeRegion));
                }
            }
        }

        private PipelineFilterMessages.FormatChangedMessage CreateOutputFormatMessage(DownscalingSizes outputFormat)
        {
            return new PipelineFilterMessages.FormatChangedMessage(outputFormat.Id, outputFormat.FinalOutputSize, outputFormat.Format);
        }

        private bool IsReadyForWork()
        {
            return inputFormat != null && outputSettings.Values.Any(settings => settings != null);
        }
    }
}
