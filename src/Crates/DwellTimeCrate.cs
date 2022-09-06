using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActorPipeline.Crates
{
    internal readonly struct DwellTimeCrate
    {
        public long DwellTimeNanoseconds { get; }
        public string Description { get; }

        public DwellTimeCrate(long dwellTimeNanoseconds, string description)
        {
            DwellTimeNanoseconds = dwellTimeNanoseconds;
            Description = description;
        }
    }
}
