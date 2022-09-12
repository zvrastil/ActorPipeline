using ActorPipeline.Images2;

namespace ActorPipeline.Actors.Lut
{
    internal interface ILutTable
    {
        byte[] ProcessData(byte[] data);
        byte[] ProcessImage(IImage image);
    }
}
