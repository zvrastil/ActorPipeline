using ActorPipeline.Images2;

namespace ActorPipeline.Actors.ImageGenerators
{
    internal class FakeImageGenerator : IImageGenerator
    {
        private readonly IImage targetImage;

        public FakeImageGenerator(IImage targetImage)
        {
            this.targetImage = targetImage;
            Reset(0);
        }

        public ImagePosition Position { get; private set; }

        public int Frame { get; private set; }

        public void GenerateNextPixels(int numberOfPixels)
        {
            int currentIndex = Position.X + Position.Y * targetImage.Width;
            int newIndex = (currentIndex + numberOfPixels);
            if (newIndex > targetImage.Width * targetImage.Height)
            {
                Frame++;
                newIndex %= (targetImage.Width * targetImage.Height);
            }
            Position = new ImagePosition(newIndex % targetImage.Width, newIndex / targetImage.Height);
        }

        public void Reset(double _)
        {
            Position = new ImagePosition(0, 0);
        }

        public void Dispose()
        {
        }
    }
}
