using ActorPipeline.Images2;
using ActorPipeline.Messages;
using Akka.Actor;

namespace ActorPipeline.Actors
{
    public sealed class ImageDisplayActor : ProcessingFilterActorBase
    {
        private readonly IActorRef imageDownscalerActor;
        private DisplayMessages.SetDisplaySizeMessage displaySize;
        private DisplayMessages.SetDigitalZoomMessage digitalZoom;

        public ImageDisplayActor(IActorRef imageDownscalerActor) : base()
        {
            this.imageDownscalerActor = imageDownscalerActor;
            BaseMessages();
            Receive<DisplayMessages.SetDisplaySizeMessage>(HandleDisplaySize);
            Receive<DisplayMessages.SetDigitalZoomMessage>(HandleDigitalZoom);
        }

        private void HandleDigitalZoom(DisplayMessages.SetDigitalZoomMessage message)
        {
            LogMessage(message);
            digitalZoom = message;
            TellDownscalerIfReady();
        }

        private void HandleDisplaySize(DisplayMessages.SetDisplaySizeMessage message)
        {
            LogMessage(message);
            displaySize = message;
            TellDownscalerIfReady();
        }

        private void TellDownscalerIfReady()
        {
            if (digitalZoom != null && displaySize != null)
            {
                var region = FractionalImageRegion.WholeImage().ZoomIn(digitalZoom.ZoomFactor).PanSafe(digitalZoom.PanX, digitalZoom.PanY);
                imageDownscalerActor.Tell(new DisplayMessages.SetDisplaySettingsMessage(region, displaySize.DisplaySize));
            }
        }
    }
}
