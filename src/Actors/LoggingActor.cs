using Akka.Actor;
using Akka.Event;

namespace ActorPipeline.Actors
{
    public class LoggingActor : ReceiveActor
    {
        protected readonly ILoggingAdapter logger = Context.GetLogger();

        protected void LogMessage<T>(T message, string additionalInfo = null)
        {
            logger.Debug("{Actor}.{Message}"+(additionalInfo == null ? "" : " "+additionalInfo), Self.Path.Name, message.ToString());
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new AllForOneStrategy(_ => Directive.Escalate);
        }
    }
}
