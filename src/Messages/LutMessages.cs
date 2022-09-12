using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActorPipeline.Messages
{
    public class LutMessages
    {
        public sealed class SetDigitalCBMessage
        {
            public double Contrast { get; }
            public double Brightness { get; }
            public double Gamma { get; }

            public SetDigitalCBMessage(double contrast, double brightness, double gamma)
            {
                Contrast = contrast;
                Brightness = brightness;
                Gamma = gamma;
            }
        }
    }
}
