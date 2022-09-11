using System;
using System.Collections.Generic;
using System.Linq;
using ActorPipeline.Images2;
using ActorPipeline.Messages;
using Akka.Actor;

namespace ActorPipeline.Actors
{
    public sealed class ImageDisplayPipelineActor : LoggingActor
    {
        private readonly List<IActorRef> filters = new List<IActorRef>();
        private readonly IActorRef imageDownscalerActor;
        private readonly IActorRef renderingActor;
        private DisplayMessages.SetDisplaySizeMessage displaySize;
        private DisplayMessages.SetDigitalZoomMessage digitalZoom;

        public ImageDisplayPipelineActor(IActorRef imageDownscalerActor, IActorRef renderingActor) : base()
        {
            this.imageDownscalerActor = imageDownscalerActor;
            this.renderingActor = renderingActor;
            Receive<DisplayMessages.AddFilterMessage>(HandleAddFilter);
            Receive<PipelineFilterMessages.FormatChangedMessage>(ForwardToHead);
            Receive<PipelineFilterMessages.PartialUpdateMessage>(ForwardToHead);

            Receive<DisplayMessages.SetDisplaySizeMessage>(HandleDisplaySize);
            Receive<DisplayMessages.SetDigitalZoomMessage>(HandleDigitalZoom);
            Receive<DisplayMessages.ZoomIntoMessage>(HandleZoomInto);
            Receive<DisplayMessages.PanByMessage>(HandlePanBy);
            Receive<DisplayMessages.GetZoomedRegionMessage>(HandleGetZoomedRegion);
            renderingActor.Tell(new DisplayMessages.SetImageDisplayPipelineMessage(Self));
            imageDownscalerActor.Tell(new PipelineFilterMessages.SubscribeMessage(Self));
        }        

        private void HandleAddFilter(DisplayMessages.AddFilterMessage message)
        {
            LogMessage(message);

            var newFilter = Context.ActorOf(message.Props, message.FilterName);
            if (filters.Any())
            {                
                filters.Last().Tell(new PipelineFilterMessages.UnsubscribeMessage(renderingActor));
                newFilter.Tell(new PipelineFilterMessages.ConnectToMessage(filters.Last()));
            }
            newFilter.Tell(new PipelineFilterMessages.SubscribeMessage(renderingActor));
            filters.Add(newFilter);           
            Sender.Tell(newFilter);
        }        

        private void ForwardToHead<T>(T message)
        {
            LogMessage(message);
            if (filters.Any())
            {
                filters.First().Tell(message);
            }
            else
            {
                renderingActor.Tell(message);
            }
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
            targetCenter = LimitPanToZoomFactor(targetZoom, targetCenter);

            var targetRegion = FractionalImageRegion.WholeImage().ZoomIn(targetZoom).PanSafe(targetCenter.X, targetCenter.Y);
            if (targetRegion != region)
            {
                digitalZoom = new DisplayMessages.SetDigitalZoomMessage(targetZoom, targetCenter.X, targetCenter.Y);
                TellDownscalerIfReady();
            }
        }        

        private void HandlePanBy(DisplayMessages.PanByMessage message)
        {
            LogMessage(message);
            DisplayMessages.SetDigitalZoomMessage currentZoom =
                digitalZoom ?? new DisplayMessages.SetDigitalZoomMessage(1, 0.5, 0.5);

            var region = FractionalImageRegion.WholeImage().ZoomIn(currentZoom.ZoomFactor).PanSafe(currentZoom.PanX, currentZoom.PanY);

            var newPan = LimitPanToZoomFactor(currentZoom.ZoomFactor, new FractionalImagePosition(
                currentZoom.PanX + message.DeltaX / currentZoom.ZoomFactor,
                currentZoom.PanY + message.DeltaY / currentZoom.ZoomFactor));

            var targetRegion = FractionalImageRegion.WholeImage().ZoomIn(currentZoom.ZoomFactor).PanSafe(newPan.X, newPan.Y);
            if (targetRegion != region)
            {
                digitalZoom = new DisplayMessages.SetDigitalZoomMessage(currentZoom.ZoomFactor, newPan.X, newPan.Y);
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

        private void HandleGetZoomedRegion(DisplayMessages.GetZoomedRegionMessage message)
        {
            LogMessage(message);
            DisplayMessages.SetDigitalZoomMessage currentZoom =
                digitalZoom ?? new DisplayMessages.SetDigitalZoomMessage(1, 0.5, 0.5);
            var region = FractionalImageRegion.WholeImage().ZoomIn(currentZoom.ZoomFactor).PanSafe(currentZoom.PanX, currentZoom.PanY);
            Sender.Tell(region);
        }

        private void TellDownscalerIfReady()
        {
            if (digitalZoom != null && displaySize != null)
            {
                var region = FractionalImageRegion.WholeImage().ZoomIn(digitalZoom.ZoomFactor).PanSafe(digitalZoom.PanX, digitalZoom.PanY);
                imageDownscalerActor.Tell(new DisplayMessages.SetDisplaySettingsMessage(region, displaySize.DisplaySize));
            }
        }

        private static FractionalImagePosition LimitPanToZoomFactor(double targetZoom, FractionalImagePosition pan)
        {
            double zoomedWidth = 1.0 / targetZoom;
            double minPan = zoomedWidth / 2.0;
            double maxPan = 1.0 - zoomedWidth / 2.0;
            return new FractionalImagePosition(
                Math.Max(minPan, Math.Min(pan.X, maxPan)),
                Math.Max(minPan, Math.Min(pan.Y, maxPan))
                );
        }
    }
}
