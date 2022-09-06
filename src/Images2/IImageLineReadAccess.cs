using System;

namespace ActorPipeline.Images2
{
    public interface IImageLineReadAccess : IDisposable
    {
        int Row { get; }
        IntPtr Ptr { get; }
    }
}
