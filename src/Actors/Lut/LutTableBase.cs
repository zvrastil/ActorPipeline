using System;
using ActorPipeline.Images2;

namespace ActorPipeline.Actors.Lut
{
    public abstract class LutTableBase : ILutTable
    {
        private readonly double contrast;
        private readonly double brightness;
        private readonly double gamma;

        protected LutTableBase(double contrast, double brightness, double gamma)
        {
            this.contrast = contrast;
            this.brightness = brightness;
            this.gamma = gamma;
        }

        protected double Transform(double value)
        {
            double y = Math.Pow(value, gamma) - 0.5 + brightness;
            y = y * contrast + 0.5;
            return Math.Max(0.0, Math.Min(1.0, y));
        }

        public abstract byte[] ProcessData(byte[] data);
        public abstract byte[] ProcessImage(IImage image);
    }
}
