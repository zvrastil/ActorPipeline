using Akka.Actor;
using Akka.Dispatch.MessageQueues;

namespace ActorPipeline.Mailbox
{
    public class ReplaceableMessageQueue : IMessageQueue
    {
        private readonly string ownerName;
        //private readonly ILoggingAdapter logger;
        private Envelope[] queue;
        private int front, rear;
        private readonly object queueLock = new object();

        public ReplaceableMessageQueue(string ownerName, int initialCapacity = 32)
        {
            this.ownerName = ownerName;
            queue = new Envelope[initialCapacity];
            front = rear = Count = 0;
            //logger = Logging.GetLogger(ActorGlobals.ActorSystem, this);
        }

        public void Enqueue(IActorRef receiver, Envelope envelope)
        {
            lock (queueLock)
            {
                if (envelope.Message is IReplaceableMessage replaceableMessage)
                {
                    EnqueueReplaceable(envelope, replaceableMessage);
                }
                else
                {
                    EnqueueStandard(envelope);
                }
            }
        }

        public bool TryDequeue(out Envelope envelope)
        {
            envelope = default;
            lock (queueLock)
            {
                if (Count == 0)
                {
                    return false;
                }

                envelope = queue[front];
                queue[front] = default; // clear to allow possible garbage collection
                front = (front+1) % queue.Length;
                Count--;
                /*if (Count > 0 && Count % 10 == 0)
                {
                    logger.Log(LogLevel.InfoLevel, $"Queue of {ownerName} reached {Count}");
                }*/
                return true;
            }
        }

        public void CleanUp(IActorRef owner, IMessageQueue deadletters)
        {
            lock (queueLock)
            {
                while (TryDequeue(out var envelope))
                {
                    deadletters.Enqueue(owner, envelope);
                }
            }
        }

        public bool HasMessages
        {
            get
            {
                lock (queueLock)
                {
                    return Count > 0;
                }
            }
        } 

        public int Count { get; private set; }

        private void EnqueueReplaceable(Envelope envelope, IReplaceableMessage replaceableMessage)
        {
            int index = front;
            while (index != rear)
            {
                if (queue[index].Message is IReplaceableMessage older && replaceableMessage.CanReplace(older))
                {
                    queue[index] = envelope;
                    return;
                }

                index = (index + 1) % queue.Length;
            }
            EnqueueStandard(envelope);
        }

        private void EnqueueStandard(Envelope envelope)
        {
            DoubleIfNecessary();

            queue[rear] = envelope;
            rear = (rear + 1) % queue.Length;
            Count++;
            /*if (Count % 10 == 0)
            {
                logger.Log(LogLevel.WarningLevel, $"Queue of {ownerName} reached {Count}");
            }*/
        }

        private void DoubleIfNecessary()
        {
            if (Count == queue.Length)
            {
                var newQueue = new Envelope[queue.Length * 2];
                int index = front;
                for (int i = 0; i < Count; i++)
                {
                    newQueue[i] = queue[index];
                    index = (index + 1) % queue.Length;
                }

                front = 0;
                rear = Count;
                queue = newQueue;
            }
        }
    }
}
