
using ActorPipeline.Messages;
using ActorPipeline.Actors.Lut;
using ActorPipeline.Images2;
using ActorPipeline.Images2.Factories;

namespace ActorPipeline.Actors
{
    public sealed class LutFilterActor : ProcessingFilterActorBase
    {
        private double contrast = 1.0;
        private double brightness = 0.0;
        private double gamma = 1.0;

        private ILutTable lutTable;
        private IImage unprocessedImage;
        private readonly IImageFactory imageFactory;


        public LutFilterActor()
        {
            imageFactory = new InMemoryImageFactory();
            BaseMessages();
            Receive<LutMessages.SetDigitalCBMessage>(HandleSetDigitalContrastAndBrightness);
        }

        protected override void PostStop()
        {
            unprocessedImage?.Dispose();
            base.PostStop();
        }

        protected override void HandleFormatChange(PipelineFilterMessages.FormatChangedMessage message)
        {
            base.HandleFormatChange(message);
            unprocessedImage?.Dispose();
            unprocessedImage = imageFactory.Create(message.Size, message.Format);
            UpdateLutTable();
        }

        protected override void HandleImageUpdate(PipelineFilterMessages.PartialUpdateMessage message)
        {
            if (message.FormatChangeId != currentFormat.Id)
            {
                return;
            }
            LogMessage(message);
            unprocessedImage.AddDataToRegion(message.Changed, message.Data);
            var newMessage = message;
            if (lutTable != null)
            {
                newMessage = new PipelineFilterMessages.PartialUpdateMessage(message.FormatChangeId, message.Changed,
                    lutTable.ProcessData(message.Data));
            }
            TellToSubscribers(newMessage);
        }

        private void HandleSetDigitalContrastAndBrightness(LutMessages.SetDigitalCBMessage message)
        {
            contrast = message.Contrast;
            brightness = message.Brightness;
            gamma = message.Gamma;
            UpdateLutTable();
            PerformFullUpdateIfNeeded();
        }

        private void PerformFullUpdateIfNeeded()
        {
            byte[] data;
            if (lutTable == null)
            {
                data = unprocessedImage.ExtractData(ImageRegion.WholeImage(unprocessedImage));
            }
            else
            {
                data = lutTable.ProcessImage(unprocessedImage);
            }

            var message = new PipelineFilterMessages.PartialUpdateMessage(
                currentFormat.Id, ImageRegion.WholeImage(unprocessedImage), data
            );
            TellToSubscribers(message);
        }

        private bool IsOff => contrast == 1.0 && brightness == 0.0 && gamma == 1.0;

        private void UpdateLutTable()
        {
            if (!IsOff && currentFormat != null)
            {
                lutTable = LutTableFactory.Create(currentFormat.Format, contrast, brightness, gamma);
            }
            else
            {
                lutTable = null;
            }
        }
    }
}
