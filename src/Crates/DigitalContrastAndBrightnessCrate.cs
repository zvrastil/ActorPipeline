using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActorPipeline.Crates
{
    internal readonly struct DigitalContrastAndBrightnessCrate
    {
        public string Name { get; }
        public double Contrast { get; }
        public double Brightness { get; }
        public double Gamma { get; }

        public DigitalContrastAndBrightnessCrate(string name, double contrast, double brightness, double gamma)
        {
            Name = name;
            Contrast = contrast;
            Brightness = brightness;
            Gamma = gamma;
        }
    }
}
