using System.Runtime.InteropServices;

namespace ActorPipeline.Images2
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RgbType
    {
        public RgbType(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        public byte R;
        public byte G;
        public byte B;
    }
}
