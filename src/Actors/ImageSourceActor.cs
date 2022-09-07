using ActorPipeline.Actors.ImageGenerators;
using ActorPipeline.Images2;
using ActorPipeline.Messages;
using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ActorPipeline.Images2.Factories;
using Serilog.Core;

namespace ActorPipeline.Actors
{
    public class ImageSourceActor : FilterActorBase
    {

        private static readonly TimeSpan updateInterval = TimeSpan.FromMilliseconds(100);

        private IImage sourceImage;
        private IImageGenerator imageGenerator;
        private long dwellTimeNanoseconds = 50;
        private DateTime lastTick;
        private ImagePosition lastPosition;
        private long lastFrame;

        #region Private Messages

        private sealed class TimerTickMessage
        {
            public override string ToString() => "TimerTick";
        }

        #endregion

        private ICancelable timer;

        public ImageSourceActor() : base(FilterFlags.SourceFilter)
        {
            IdleMessages();            
        }

        private void CommonMessages()
        {
            BaseMessages();
            Receive<PipelineControlMessages.SetImageFormatMessage>(HandleFormatChange);
            Receive<PipelineControlMessages.SetDwellTimeMessage>(HandleDwellTimeChange);
        }        

        private void IdleMessages()
        {
            CommonMessages();
            Receive<PipelineControlMessages.StartMessage>(HandleStart);
        }

        private void RunningMessages()
        {
            CommonMessages();
            Receive<TimerTickMessage>(HandleTimerTick);
            Receive<PipelineControlMessages.StopMessage>(HandleStop);
        }        

        private void HandleStart(PipelineControlMessages.StartMessage message)
        {
            LogMessage(message);
            if (!IsImageFormatSet())
            {
                throw new InvalidOperationException("Can't start pipeline when format is not known.");
            }
            Become(RunningMessages);
            lastTick = DateTime.Now;
            timer = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
                updateInterval, 
                updateInterval,
                Self, 
                new TimerTickMessage(), 
                Self);
        }        

        private void HandleStop(PipelineControlMessages.StopMessage message)
        {
            LogMessage(message);
            Become(IdleMessages);
            timer.Cancel();
        }

        private void HandleTimerTick(TimerTickMessage message)
        {
            LogMessage(message);
            TimeSpan delta = DateTime.Now - lastTick;
            lastTick = DateTime.Now;

            int pixelsToChange =  Math.Min(sourceImage.Width*sourceImage.Height, (int)(delta.TotalSeconds * 1e9 / dwellTimeNanoseconds));
            //int pixelsToChange = (int)(delta.TotalSeconds * 1e9 / dwellTimeNanoseconds);
            imageGenerator.GenerateNextPixels(pixelsToChange);

            if (lastPosition.Y != imageGenerator.Position.Y || lastFrame != imageGenerator.Frame)
            {
                UpdateFullLines();
            }
        }

        private void UpdateFullLines()
        {
            foreach (var range in GetUpdateRegions())
            {
                var message = CreatePartialUpdate(range.Item1, range.Item2);
                TellToSubscribers(message);
            }
            lastPosition = imageGenerator.Position;
            lastFrame = imageGenerator.Frame;
        }

        private IEnumerable<(int, int)> GetUpdateRegions()
        {
            if (lastFrame == imageGenerator.Frame)
            {
                yield return (lastPosition.Y, imageGenerator.Position.Y - lastPosition.Y);
            }
            else
            {
                if (lastPosition.Y < sourceImage.Height-1 || lastPosition.X < sourceImage.Width-1)
                {
                    yield return (lastPosition.Y, sourceImage.Height - lastPosition.Y);
                }
                if (imageGenerator.Position.Y > 0 || imageGenerator.Position.X == sourceImage.Width-1)
                {
                    yield return (0, imageGenerator.Position.Y);
                }
            }
        }

        private PipelineFilterMessages.PartialUpdateMessage CreatePartialUpdate(int fromY, int height)
        {
            var rect = new ImageRegion(0, fromY, sourceImage.Width, height);
            return new PipelineFilterMessages.PartialUpdateMessage(rect, ExtractData(sourceImage, rect));
        }

        private static byte[] ExtractData(IImage image, ImageRegion rect)
        {
            byte[] result = new byte[rect.Width * rect.Height * image.BytesPerPixel];
            for (int i = 0; i < rect.Height; i++)
            {
                using (var readAccess = image.LockLineForReading(rect.Top + i))
                {
                    Marshal.Copy(readAccess.Ptr+rect.Left*image.BytesPerPixel, result, i*rect.Width*image.BytesPerPixel, rect.Width*image.BytesPerPixel);
                }
            }

            return result;
        }

        private static double GetNoiseLevel(long dwellTime)
        {
            double noiseCancellationFactor = Math.Log(dwellTime);
            return 1.0/noiseCancellationFactor;
        }

        private void HandleFormatChange(PipelineControlMessages.SetImageFormatMessage message)
        {
            LogMessage(message);
            sourceImage?.Dispose();
            sourceImage = InMemoryImageFactory.Instance.Create(message.Size, message.Format);
            imageGenerator?.Dispose();
            imageGenerator = new MandelbrotImageGenerator(sourceImage, GetNoiseLevel(dwellTimeNanoseconds));
            lastTick = DateTime.Now;
            lastPosition = new ImagePosition(0, 0);
            TellToSubscribers(new PipelineFilterMessages.FormatChangedMessage(sourceImage));
        }

        private void HandleDwellTimeChange(PipelineControlMessages.SetDwellTimeMessage message)
        {
            LogMessage(message);
            dwellTimeNanoseconds = message.DwellTimeNanoseconds;
            imageGenerator?.Reset(GetNoiseLevel(dwellTimeNanoseconds));
            lastTick = DateTime.Now;    
            lastPosition = new ImagePosition(0, 0);
        }

        protected override void OnSubscribed(IActorRef actor)
        {
            base.OnSubscribed(actor);
            if (IsImageFormatSet())
            {
                actor.Tell(new PipelineFilterMessages.FormatChangedMessage(sourceImage));
            }
        }

        private bool IsImageFormatSet() => sourceImage != null;

                
    }
}
