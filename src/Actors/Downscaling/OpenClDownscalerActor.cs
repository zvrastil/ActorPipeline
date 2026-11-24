using System;
using System.Collections.Generic;
using ActorPipeline.Images2;
using ActorPipeline.Messages;
using Akka.Actor;
using Akka.Event;

namespace ActorPipeline.Actors.Downscaling
{
    internal class OpenClDownscalerActor : LoggingActor, IWithUnboundedStash
    {
        private readonly IActorRef parentDownscaler;

        private readonly Dictionary<IActorRef, IActorRef> outputHandlers = new Dictionary<IActorRef, IActorRef>();

        private readonly OpenClRunner clRunner;
        private readonly PendingWork pendingWork = new PendingWork();
        private ImageFormat currentFormat;
        private ImageSize inputSize = ImageSize.Invalid;        

        public OpenClDownscalerActor(IActorRef parentDownscaler)
        {
            this.parentDownscaler = parentDownscaler;

            clRunner = new OpenClRunner(logger);

            Receive<DownscalingMessages.SetDownscalingSettingsMessage>(HandleSettingsChange);
            Receive<PipelineFilterMessages.PartialUpdateMessage>(HandlePartialUpdate);
            Receive<DownscalingMessages.ResizeCompletedMessage>(HandleResizeCompleted);
        }

        public IStash Stash { get; set; }

        protected override void PostStop()
        {
            clRunner?.Dispose();
            base.PostStop();
        }

        private void HandleResizeCompleted(DownscalingMessages.ResizeCompletedMessage message)
        {
            LogMessage(message);
            pendingWork.WorkCompleted(Sender);
            if (pendingWork.IsEmpty)
            {
                Stash.UnstashAll();
            }
        }

        private void HandleSettingsChange(DownscalingMessages.SetDownscalingSettingsMessage message)
        {           
            if (!pendingWork.IsEmpty)
            {
                logger.Debug("  stashing settings change");
                Stash.Stash();
            }
            else
            {
                LogMessage(message);
                ApplySettingsChange(message);
            }
        }

        private void HandlePartialUpdate(PipelineFilterMessages.PartialUpdateMessage message)
        {
            LogMessage(message);
            if (!pendingWork.IsEmpty)
            {
                logger.Debug("  stashing partial update");
                Stash.Stash();
            }
            else
            {
                PerformPartialUpdateAndResize(message);
            }
        }

        private void ApplySettingsChange(DownscalingMessages.SetDownscalingSettingsMessage message)
        {
            var finalSizes = new DownscalingSizes(MessageIdSource.NextId(), message.InputSize,
                                CalculateInputRegion(message), CalculateOutputSize(message), message.Format);

            HandleSizesChange(message.ForActor, finalSizes);
        }        

        private void HandleSizesChange(IActorRef forActor, DownscalingSizes sizes)
        {
            LogMessage(sizes, "for "+forActor.Path.Name);            

            if (InputBufferFormatChanged(sizes))
            {
                CreateNewInputBuffer(sizes);
            }

            currentFormat = sizes.Format;

            if (!outputHandlers.TryGetValue(forActor, out var outputHandler))
            {
                outputHandler = Context.ActorOf(Props.Create(() => new OutputHandlingActor(clRunner, forActor)), "outputHandler_" + forActor.Path.Name);
                outputHandlers.Add(forActor, outputHandler);
            }

            pendingWork.AddWork(outputHandler);
            outputHandler.Tell(new DownscalingMessages.SetDownscalingSizesMessage(forActor, sizes));            
        }        

        private void PerformPartialUpdateAndResize(PipelineFilterMessages.PartialUpdateMessage message)
        {
            CopyDataToInputBuffer(message);
                       
            foreach (var outputHandler in outputHandlers.Values)
            {
                pendingWork.AddWork(outputHandler);
                outputHandler.Tell(new DownscalingMessages.PartialResizeMessage(message.Changed));
            }                       
        }

        private void CopyDataToInputBuffer(PipelineFilterMessages.PartialUpdateMessage message)
        {
            clRunner.CopyToInputBuffer(message.Changed, message.Data);
        }

        private bool InputBufferFormatChanged(DownscalingSizes sizes)
        {
            return (inputSize == ImageSize.Invalid ||
                    sizes.Format != currentFormat ||
                    sizes.InputSize.Width != inputSize.Width ||
                    sizes.InputSize.Height != inputSize.Height);
        }

        private void CreateNewInputBuffer(DownscalingSizes sizes)
        {
            inputSize = sizes.InputSize;
            clRunner.CreateInputBuffer(sizes.Format, sizes.InputSize.Width, sizes.InputSize.Height);
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
