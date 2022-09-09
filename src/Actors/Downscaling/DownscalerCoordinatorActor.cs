using System;
using System.Collections.Generic;
using System.Linq;
using ActorPipeline.Images2;
using ActorPipeline.Messages;
using Akka.Actor;
using Cloo;

namespace ActorPipeline.Actors.Downscaling
{
    internal class DownscalerCoordinatorActor : LoggingActor
    {
        private readonly IActorRef parentDownscaler;
        private readonly HashSet<IActorRef> downscalers = new HashSet<IActorRef>();

        private ComputeContext openClContext;


        public DownscalerCoordinatorActor(IActorRef parentDownscaler)
        {
            this.parentDownscaler = parentDownscaler;

            var platform = ComputePlatform.Platforms.First();
            var device = platform.Devices.First(d => d.Type == ComputeDeviceTypes.Gpu);
            openClContext = new ComputeContext(new[] {device}, new ComputeContextPropertyList(platform), null, IntPtr.Zero);

            downscalers.Add(Context.ActorOf(Props.Create(() => new OpenClDownscalerActor(Self, openClContext)), "openClDownscaler"));
            Receive<DownscalingMessages.SetDownscalingSettingsMessage>(HandleSettingsChange);
            Receive<PipelineFilterMessages.PartialUpdateMessage>(HandlePartialUpdate);
            Receive<DownscalingMessages.OutputUpdatedMessage>(HandleOutputUpdate);
        }

        protected override void PostStop()
        {
            openClContext?.Dispose();
            openClContext = null;
            base.PostStop();
        }

        private void HandleOutputUpdate(DownscalingMessages.OutputUpdatedMessage message)
        {
            LogMessage(message);
            // combine data from multiple downscalers when we have them
            // now, just forward to parent
            parentDownscaler.Tell(message);
        }

        private void HandleSettingsChange(DownscalingMessages.SetDownscalingSettingsMessage message)
        {
            LogMessage(message);

            var finalSizesMessage = new DownscalingMessages.SetDownscalingSizesMessage(MessageIdSource.NextId(), message.InputSize,
                CalculateInputRegion(message), CalculateOutputSize(message), message.Format);

            parentDownscaler.Tell(finalSizesMessage);

            foreach (var downscaler in downscalers)
            {
                downscaler.Tell(finalSizesMessage);
            }
        }

        private void HandlePartialUpdate(PipelineFilterMessages.PartialUpdateMessage message)
        {
            LogMessage(message);

            foreach (var downscaler in downscalers)
            {
                downscaler.Tell(message);
            }
        }

        private static FractionalImageRegion CalculateInputRegion(DownscalingMessages.SetDownscalingSettingsMessage message)
        {
            var mustSeeRegion = message.ZoomedArea.ToFractionalPixels(message.InputSize);
            double scale = Math.Min(message.DisplaySize.Width / mustSeeRegion.Width, message.DisplaySize.Height / mustSeeRegion.Height);
            ImageSize idealSize = new ImageSize((int)(message.DisplaySize.Width / scale + 0.5), (int)(message.DisplaySize.Height / scale + 0.5));
            return mustSeeRegion.ExpandSafelyTo(idealSize, message.InputSize);
        }

        private static ImageSize CalculateOutputSize(DownscalingMessages.SetDownscalingSettingsMessage message)
        {
            FractionalImageSize inputRegionSize = CalculateInputRegion(message).Size;
            double scale = Math.Min((double)message.DisplaySize.Width / inputRegionSize.Width, (double)message.DisplaySize.Height / inputRegionSize.Height);
            if (scale > 1.0)
            {
                scale = 1.0;
            }
            return new ImageSize((int)(scale * inputRegionSize.Width + 0.5), (int)(scale * inputRegionSize.Height + 0.5));
        }

    }
}
