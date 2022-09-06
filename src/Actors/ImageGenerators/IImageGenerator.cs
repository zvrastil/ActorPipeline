using System;
using ActorPipeline.Images2;

namespace ActorPipeline.Actors.ImageGenerators
{
    internal interface IImageGenerator : IDisposable
    {
        ImagePosition Position { get; }
        int Frame { get; }
        void Reset(double noiseLevel);
        void GenerateNextPixels(int numberOfPixels);

    }
}
