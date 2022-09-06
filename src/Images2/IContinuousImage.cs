using System;

namespace ActorPipeline.Images2
{
    public interface IContinuousImage : IImage
    {
        IntPtr ImageData { get; }
        int Stride { get; }
    }
}
