using System.Runtime.InteropServices;

namespace ActorPipeline.Images2
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BgraType
    {
        public BgraType(byte b, byte g, byte r)
        {
            B = b;
            G = g;
            R = r;
            A = 0;
        }

        public byte B;
        public byte G;
        public byte R;
        public byte A;
    }
}
