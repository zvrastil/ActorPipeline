using System.Threading;

namespace ActorPipeline.Messages
{
    public static class MessageIdSource
    {
        private static long idBase = 0;

        public static long NextId() => Interlocked.Increment(ref idBase);
    }
}
