using Akka.Actor;
using Akka.Configuration;
using Akka.Dispatch;
using Akka.Dispatch.MessageQueues;

namespace ActorPipeline.Mailbox
{
    public sealed class ReplaceableMessageMailbox : MailboxType, IProducesMessageQueue<ReplaceableMessageQueue>
    {
        public ReplaceableMessageMailbox(Settings settings, Config config) : base(settings, config)
        {
        }

        public override IMessageQueue Create(IActorRef owner, ActorSystem system)
        {
            return new ReplaceableMessageQueue();
        }
    }
}
