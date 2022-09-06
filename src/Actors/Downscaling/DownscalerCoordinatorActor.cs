using System;
using System.Collections.Generic;
using System.Linq;
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
        }

        protected override void PostStop()
        {
            openClContext?.Dispose();
            openClContext = null;
            base.PostStop();
        }

        private void HandleSettingsChange(DownscalingMessages.SetDownscalingSettingsMessage message)
        {
            foreach (var downscaler in downscalers)
            {
                downscaler.Tell(message);
            }
        }

        private void HandlePartialUpdate(PipelineFilterMessages.PartialUpdateMessage message)
        {
            foreach (var downscaler in downscalers)
            {
                downscaler.Tell(message);
            }
        }

        
    }
}
