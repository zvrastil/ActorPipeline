using ActorPipeline.Images;

namespace ActorPipeline.Actors.ImageGenerators
{
    internal class FakeImageGenerator : IImageGenerator
    {
        private readonly IImage targetImage;

        public FakeImageGenerator(IImage targetImage)
        {
            this.targetImage = targetImage;
            Reset();
        }

        public IntPosition Position { get; private set; }

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
            Position = new IntPosition(newIndex % targetImage.Width, newIndex / targetImage.Height);
        }

        public void Reset()
        {
            Position = new IntPosition(0, 0);
        }
    }
}
