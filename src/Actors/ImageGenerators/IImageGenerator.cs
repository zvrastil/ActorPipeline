using ActorPipeline.Images;

namespace ActorPipeline.Actors.ImageGenerators
{
    internal interface IImageGenerator
    {
        IntPosition Position { get; }
        int Frame { get; }
        void Reset();
        void GenerateNextPixels(int numberOfPixels);

    }
}
