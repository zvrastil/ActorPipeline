using ActorPipeline.Images2;
using ActorPipeline.Messages;
using ActorPipeline.Messages.Data;
using Akka.Actor;
using BitMiracle.LibTiff.Classic;
using System.Collections.Generic;
using System.Linq;

namespace ActorPipeline.Actors
{
    public class FullImageCollectorActor : FilterActorBase
    {
        private readonly IActorRef displayPipelineActor;
        private readonly IActorRef tailProcessingActor;
        private readonly long requestId;
        private readonly string fileName;
        private Tiff outputFile;
        private HashSet<int> pendingLines;
        private byte[] rowBuffer;

        public FullImageCollectorActor(IActorRef displayPipelineActor, IActorRef imageDownscalerActor, IActorRef tailProcessingActor, 
            long requestId, string fileName, bool zoomedAreaOnly, ZoomedImageArea area) : base(FilterFlags.TargetFilter)
        {
            this.displayPipelineActor = displayPipelineActor;
            this.tailProcessingActor = tailProcessingActor;
            this.requestId = requestId;
            this.fileName = fileName;

            tailProcessingActor.Tell(new PipelineFilterMessages.SubscribeMessage(Self));
            imageDownscalerActor.Tell(new PipelineFilterMessages.SendZoomedImageMessage(zoomedAreaOnly, area));

            Receive<PipelineFilterMessages.ImageDataMessage>(HandleImageData);
        }

        private void HandleImageData(PipelineFilterMessages.ImageDataMessage message)
        {
            LogMessage(message);
            CreateTiffIfNeeded(message.ImageSize.Width, message.ImageSize.Height, message.Format);
            WriteDataToTiff(message);
            if (pendingLines.Count == 0)
            {
                outputFile.WriteDirectory();
                outputFile.Dispose();
                displayPipelineActor.Tell(new DisplayMessages.ImageSavedMessage(requestId));
            }
        }

        private void WriteDataToTiff(PipelineFilterMessages.ImageDataMessage message)
        {
            for (int i = 0; i < message.NumberOfLines; i++)
            {
                WriteScanLine(message.Format, message.Data, i, i+message.FirstLine, message.ImageSize.Width);
                pendingLines.Remove(i + message.FirstLine);
            }
        }

        private void WriteScanLine(ImageFormat format, byte[] data, int lineInData, int y, int width)
        {
            switch (format)
            {
                case ImageFormat.Grayscale8:
                    WriteGrayscaleScanLine(data, lineInData, y, width);
                    break;
                case ImageFormat.Grayscale16:
                    WriteGrayscaleScanLine(data, lineInData, y, 2*width);
                    break;
                case ImageFormat.Bgra32:
                    WriteRgbScanLine(data, lineInData, y, width);
                    break;
            }
        }

        private void WriteGrayscaleScanLine(byte[] data, int lineInData, int y, int stride)
        {
            outputFile.WriteScanline(data, lineInData * stride, y, 0);
        }

        private unsafe void WriteRgbScanLine(byte[] data, int lineInData, int y, int width)
        {
            if (rowBuffer == null)
            {
                rowBuffer = new byte[width * 3];
            }
            int offset = lineInData * width * 4;
            for (int i = 0; i < width; i++)
            {
                rowBuffer[i * 3] = data[offset + i * 4 + 2];
                rowBuffer[i * 3 + 1] = data[offset + i * 4 + 1];
                rowBuffer[i * 3 + 2] = data[offset + i * 4];
            }

            outputFile.WriteScanline(rowBuffer, y);
        }

        private void CreateTiffIfNeeded(int width, int height, ImageFormat format)
        {
            if (outputFile == null)
            {
                outputFile = Tiff.Open(fileName, "w");
                outputFile.SetField(TiffTag.IMAGEWIDTH, width);
                outputFile.SetField(TiffTag.IMAGELENGTH, height);
                outputFile.SetField(TiffTag.SAMPLESPERPIXEL, format == ImageFormat.Bgra32 ? 3 : 1);
                outputFile.SetField(TiffTag.BITSPERSAMPLE, format == ImageFormat.Grayscale16 ? 16 : 8);
                outputFile.SetField(TiffTag.ORIENTATION, Orientation.TOPLEFT);
                outputFile.SetField(TiffTag.ROWSPERSTRIP, height);
                outputFile.SetField(TiffTag.XRESOLUTION, 96.0);
                outputFile.SetField(TiffTag.YRESOLUTION, 96.0);
                outputFile.SetField(TiffTag.RESOLUTIONUNIT, ResUnit.INCH);
                outputFile.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
                outputFile.SetField(TiffTag.PHOTOMETRIC, format == ImageFormat.Bgra32 ? Photometric.RGB : Photometric.MINISBLACK);
                outputFile.SetField(TiffTag.COMPRESSION, Compression.NONE);
                outputFile.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);

                pendingLines = new HashSet<int>(Enumerable.Range(0, height));
            }
        }

        protected override void PostStop()
        {
            tailProcessingActor.Tell(new PipelineFilterMessages.UnsubscribeMessage(Self));
            base.PostStop();
        }


    }
}
