using ActorPipeline.Images2;

namespace ActorPipeline.Actors.ImageGenerators
{
    internal abstract class BaseImageGenerator : IImageGenerator
    {
        protected readonly IImage targetImage;
        private long currentPosition;
        private readonly long dataSize;
        private IWriter writer;

        protected BaseImageGenerator(IImage targetImage)
        {
            this.targetImage = targetImage;
            dataSize = targetImage.Width * targetImage.Height;
            Reset(0);
        }

        protected abstract IWriter CreateWriter();

        public ImagePosition Position { get; private set; }

        public int Frame { get; private set; }

        public void GenerateNextPixels(int numberOfPixels)
        {
            for (int i = 0; i < numberOfPixels; i++)
            {
                writer.WriteNext();
                currentPosition++;
                if (currentPosition >= dataSize)
                {
                    currentPosition = 0;
                    Frame++;
                }
            }
            Position = new ImagePosition((int)(currentPosition % targetImage.Width), (int)(currentPosition / targetImage.Width));
        }

        public void Reset(double _)
        {
            currentPosition = 0;
            Frame = 0;
            writer = CreateWriter();
        }

        internal interface IWriter
        {
            void WriteNext();
        }

        public void Dispose()
        {
        }
    }
}
