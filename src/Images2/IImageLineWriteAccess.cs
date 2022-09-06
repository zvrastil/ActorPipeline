using System;

namespace ActorPipeline.Images2
{
    public interface IImageLineWriteAccess : IDisposable
    {
        int Row { get; }
        IntPtr Ptr { get; }
        void Commit();
    }
}
