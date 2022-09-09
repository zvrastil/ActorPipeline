using System;
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
            Receive<DisplayMessages.ZoomIntoMessage>(HandleZoomInto);
        }

        private void HandleZoomInto(DisplayMessages.ZoomIntoMessage message)
        {
            LogMessage(message);
            DisplayMessages.SetDigitalZoomMessage currentZoom =
                digitalZoom ?? new DisplayMessages.SetDigitalZoomMessage(1, 0.5, 0.5);

            var region = FractionalImageRegion.WholeImage().ZoomIn(currentZoom.ZoomFactor).PanSafe(currentZoom.PanX, currentZoom.PanY);

            var targetZoom = Math.Max(1.0, Math.Min(64.0, currentZoom.ZoomFactor * Math.Pow(2, message.Delta)));

            var targetCenter = new FractionalImagePosition(
                message.ZoomCenter.X * region.Width + region.Left,
                message.ZoomCenter.Y * region.Height + region.Top
            );
            double dx = (currentZoom.PanX - targetCenter.X) * (currentZoom.ZoomFactor / targetZoom);
            double dy = (currentZoom.PanY - targetCenter.Y) * (currentZoom.ZoomFactor / targetZoom);
            targetCenter = new FractionalImagePosition(targetCenter.X + dx, targetCenter.Y + dy);

            var targetRegion = FractionalImageRegion.WholeImage().ZoomIn(targetZoom).PanSafe(targetCenter.X, targetCenter.Y);
            if (targetRegion != region)
            {
                digitalZoom = new DisplayMessages.SetDigitalZoomMessage(targetZoom, targetCenter.X, targetCenter.Y);
                TellDownscalerIfReady();
            }
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
