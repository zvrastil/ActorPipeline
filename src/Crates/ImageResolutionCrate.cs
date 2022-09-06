using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActorPipeline.Crates
{
    internal readonly struct ImageResolutionCrate
    {
        public int Width { get; }
        public int Height { get; }

        public string Description => $"{Width} 🞪 {Height}";

        public ImageResolutionCrate(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }
}
