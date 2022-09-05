using ActorPipeline.Messages;
using Akka.Actor;
using System.Collections.Generic;

namespace ActorPipeline.Actors
{
    public class PipelineCoordinatorActor : ReceiveActor
    {
        private readonly Dictionary<string, IActorRef> filters = new Dictionary<string, IActorRef>();
        private readonly List<IActorRef> sourceFilters = new List<IActorRef>();

        public PipelineCoordinatorActor()
        {
            Receive<PipelineControlMessages.CreateFilterMessage>(HandleCreateFilter);
            Receive<PipelineControlMessages.StartMessage>(ForwardToSourceFilters);
            Receive<PipelineControlMessages.StopMessage>(ForwardToSourceFilters);
            Receive<PipelineControlMessages.SetImageFormatMessage>(ForwardToSourceFilters);
            Receive<PipelineControlMessages.SetDwellTimeMessage>(ForwardToSourceFilters);
        }        

        private void HandleCreateFilter(PipelineControlMessages.CreateFilterMessage message)
        {
            if (filters.ContainsKey(message.FilterProperties.Name))
            {
                Sender.Tell(ActorRefs.Nobody);
            }
            var newFilter = Context.ActorOf(message.Props, message.FilterProperties.Name);
            filters.Add(message.FilterProperties.Name, newFilter);
            if (message.FilterProperties.IsSourceFilter)
            {
                sourceFilters.Add(newFilter);
            }
            Sender.Tell(newFilter);
        }

        private void ForwardToSourceFilters<T>(T message)
        {
            sourceFilters.ForEach(filter => filter.Tell(message));
        }
        
    }
}
