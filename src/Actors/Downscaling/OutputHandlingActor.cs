using ActorPipeline.Images2;
using ActorPipeline.Messages;
using Akka.Actor;
using System;

namespace ActorPipeline.Actors.Downscaling
{
    internal class OutputHandlingActor : LoggingActor
    {
        private readonly OpenClRunner clRunner;
        private readonly IActorRef forActor;
        private ImageSize outputSize = ImageSize.Invalid;
        private ImageFormat currentFormat;
        private FractionalImageRegion inputRegion;
        private long currentFormatId;

        public OutputHandlingActor(OpenClRunner clRunner, IActorRef forActor)
        {
            this.clRunner = clRunner;
            this.forActor = forActor;
            Receive<DownscalingMessages.SetDownscalingSizesMessage>(HandleSizeChange);
            Receive<DownscalingMessages.PartialResizeMessage>(HandlePartialResize);
        }

        protected override void PostStop()
        {
            clRunner.FreeOutputBuffer(forActor);
            base.PostStop();
        }

        private void HandlePartialResize(DownscalingMessages.PartialResizeMessage message)
        {
            var realUpdatedRegion = inputRegion.IntersectWith(message.UpdatedRegion);
            if (realUpdatedRegion != FractionalImageRegion.Invalid)
            {
                ImageRegion outputUpdatedRegion = CalculateOutputUpdatedRegion(realUpdatedRegion);

                var data = clRunner.PartialResize(forActor, inputRegion, outputUpdatedRegion);
                forActor.Tell(new PipelineFilterMessages.PartialUpdateMessage(currentFormatId, outputUpdatedRegion, data));
            }
            Sender.Tell(new DownscalingMessages.ResizeCompletedMessage());
        }

        private void HandleSizeChange(DownscalingMessages.SetDownscalingSizesMessage message)
        {
            LogMessage(message);

            bool needFullResize = false;

            if (OutputSizeOrFormatChanged(message.Sizes))
            {
                CreateNewOutputBuffer(message.Sizes);
                needFullResize = true;
            }

            currentFormatId = message.Sizes.Id;
            currentFormat = message.Sizes.Format;

            if (ZoomedRegionChanged(message.Sizes))
            {
                inputRegion = message.Sizes.FinalInputRegion;
                needFullResize = true;
            }

            if (needFullResize)
            {
                PerformFullResize();
            }

            Sender.Tell(new DownscalingMessages.ResizeCompletedMessage());
        }

        private void PerformFullResize()
        {            
            byte[] data = clRunner.FullResize(forActor, inputRegion);

            forActor.Tell(new PipelineFilterMessages.PartialUpdateMessage(currentFormatId, new ImageRegion(0, 0, outputSize.Width, outputSize.Height), data));
        }        

        private ImageRegion CalculateOutputUpdatedRegion(FractionalImageRegion inputUpdatedRegion)
        {
            double scale = outputSize.Width / inputRegion.Width;
            int left = Fit((int)Math.Floor((inputUpdatedRegion.Left - inputRegion.Left) * scale), 0, outputSize.Width);
            int top = Fit((int)Math.Floor((inputUpdatedRegion.Top - inputRegion.Top) * scale), 0, outputSize.Height);
            int right = Fit((int)Math.Ceiling((inputUpdatedRegion.Right - inputRegion.Left) * scale), 1, outputSize.Width);
            int bottom = Fit((int)Math.Ceiling((inputUpdatedRegion.Bottom - inputRegion.Top) * scale), 1, outputSize.Height);
            return new ImageRegion(left, top, right - left, bottom - top);
        }

        private static int Fit(int x, int min, int max) => Math.Max(min, Math.Min(x, max));

        private bool OutputSizeOrFormatChanged(DownscalingSizes sizes)
        {
            return outputSize == ImageSize.Invalid ||
                   sizes.Format != currentFormat ||
                   outputSize.Width != sizes.FinalOutputSize.Width ||
                   outputSize.Height != sizes.FinalOutputSize.Height;
        }

        private void CreateNewOutputBuffer(DownscalingSizes sizes)
        {
            outputSize = sizes.FinalOutputSize;
            clRunner.CreateOutputBuffer(forActor, sizes.Format, sizes.FinalOutputSize.Width, sizes.FinalOutputSize.Height);            
        }

        private bool ZoomedRegionChanged(DownscalingSizes sizes)
        {
            return inputRegion != sizes.FinalInputRegion;
        }
    }
}
