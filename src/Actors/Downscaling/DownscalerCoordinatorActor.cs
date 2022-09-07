using System;
using System.Collections.Generic;
using System.Linq;
using ActorPipeline.Images2;
using ActorPipeline.Messages;
using Akka.Actor;
using Cloo;

namespace ActorPipeline.Actors.Downscaling
{
    internal class DownscalerCoordinatorActor : ReceiveActor
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

            downscalers.Add(Context.ActorOf(Props.Create(() => new OpenClDownscalerActor(Self, openClContext))));
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
            // combine data from multiple downscalers when we have them
            // now, just forward to parent
            parentDownscaler.Tell(message);
        }

        private void HandleSettingsChange(DownscalingMessages.SetDownscalingSettingsMessage message)
        {
            var finalSizesMessage = new DownscalingMessages.SetDownscalingSizesMessage(new ImageSize(message.InputWidth, message.InputHeight),
                CalculateInputRegion(message), CalculateOutputSize(message), message.Format);

            parentDownscaler.Tell(finalSizesMessage);

            foreach (var downscaler in downscalers)
            {
                downscaler.Tell(finalSizesMessage);
            }
        }

        private void HandlePartialUpdate(PipelineFilterMessages.PartialUpdateMessage message)
        {
            foreach (var downscaler in downscalers)
            {
                downscaler.Tell(message);
            }
        }

        private static ImageRegion CalculateInputRegion(DownscalingMessages.SetDownscalingSettingsMessage message)
        {
            var mustSeeRegion = message.ZoomedArea.ToPixels(new ImageSize(message.InputWidth, message.InputHeight));
            double scale = Math.Min((double)message.DisplayWidth / mustSeeRegion.Width, (double)message.DisplayHeight / mustSeeRegion.Height);
            ImageSize idealSize = new ImageSize((int)(message.DisplayWidth / scale + 0.5), (int)(message.DisplayHeight / scale + 0.5));
            return mustSeeRegion.ExpandSafelyTo(idealSize, new ImageSize(message.InputWidth, message.InputHeight));
        }

        private static ImageSize CalculateOutputSize(DownscalingMessages.SetDownscalingSettingsMessage message)
        {
            ImageSize inputRegionSize = CalculateInputRegion(message).Size;
            double scale = Math.Min((double)message.DisplayWidth / inputRegionSize.Width, (double)message.DisplayHeight / inputRegionSize.Height);
            if (scale >= 1.0)
            {
                return inputRegionSize;
            }
            else
            {
                return new ImageSize((int)(scale * inputRegionSize.Width + 0.5), (int)(scale * inputRegionSize.Height + 0.5));
            }
        }

    }
}
