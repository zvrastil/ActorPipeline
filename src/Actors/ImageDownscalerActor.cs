
using ActorPipeline.Actors.Downscaling;
using ActorPipeline.Images2;
using ActorPipeline.Messages;
using Akka.Actor;
using System.Collections.Generic;
using System.Linq;
using Akka.Event;

namespace ActorPipeline.Actors
{
    public sealed class ImageDownscalerActor : FilterActorBase
    {
        private PipelineFilterMessages.FormatChangedMessage inputFormat;
        private readonly Dictionary<IActorRef, DisplayMessages.SetDisplaySettingsMessage> outputSettings = 
            new Dictionary<IActorRef, DisplayMessages.SetDisplaySettingsMessage>();
        private readonly IActorRef downscaler;

        public ImageDownscalerActor() : base(FilterFlags.ProcessingFilter)
        {
            BaseMessages();
            Receive<PipelineFilterMessages.FormatChangedMessage>(HandleFormatChange);
            Receive<DisplayMessages.SetDisplaySettingsMessage>(HandleDisplaySettings);
            Receive<PipelineFilterMessages.PartialUpdateMessage>(HandlePartialUpdate);
            Receive<DownscalingMessages.OutputUpdatedMessage>(HandleOutputUpdated);
            Receive<PipelineFilterMessages.SendZoomedImageMessage>(HandleSendZoomedImage);
            Receive<PipelineFilterMessages.ImageDataMessage>(HandleImageData);
            downscaler = Context.ActorOf(Props.Create(() => new OpenClDownscalerActor(Self)), "downscaler");
        }

        private void HandleImageData(PipelineFilterMessages.ImageDataMessage message)
        {
            // full image data, just transfer to subscribers
            LogMessage(message);
            TellToSubscribers(message);
        }

        private void HandleSendZoomedImage(PipelineFilterMessages.SendZoomedImageMessage message)
        {
            LogMessage(message);
            ImageRegion targetRegion;
            if (message.ZoomedImage)
            {
                targetRegion = FractionalImageRegion
                    .WholeImage()
                    .ZoomIn(message.Area.ZoomFactor)
                    .PanSafe(message.Area.PanX, message.Area.PanY)
                    .ToFractionalPixels(inputFormat.Size)
                    .ExpandSafelyTo(message.Area.AspectRatio, inputFormat.Size)
                    .RoundToPixels(inputFormat.Size);
            }
            else
            {
                targetRegion = new ImageRegion(0, 0, inputFormat.Size.Width, inputFormat.Size.Height);
            }

            TellToSources(new PipelineFilterMessages.SendImageDataMessage(MessageIdSource.NextId(), targetRegion));
        }

        private void HandleOutputUpdated(DownscalingMessages.OutputUpdatedMessage message)
        {
            LogMessage(message);
            message.ForActor.Tell(new PipelineFilterMessages.PartialUpdateMessage(message.OutputFormatId, message.Changed, message.Data));
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
            outputSettings[message.ForDisplay] = message;
            UpdateDownscalerSettings(message.ForDisplay);
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

        private bool IsReadyForWork()
        {
            return inputFormat != null && outputSettings.Values.Any(settings => settings != null);
        }
    }
}
