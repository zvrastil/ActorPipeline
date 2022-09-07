using Akka.Actor;
using Akka.Event;

namespace ActorPipeline.Actors
{
    public class LoggingActor : ReceiveActor
    {
        protected readonly ILoggingAdapter logger = Context.GetLogger();

        protected void LogMessage<T>(T message)
        {
            logger.Info("{Actor}.{Message}", Self.Path.Name, message.ToString());
        }
    }
}
