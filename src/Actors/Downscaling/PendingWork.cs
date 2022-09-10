using ActorPipeline.Messages;
using Akka.Actor;
using System;
using System.Collections.Generic;

namespace ActorPipeline.Actors.Downscaling
{
    internal class PendingWork
    {
        private readonly HashSet<IActorRef> pendingWorkers = new HashSet<IActorRef>();
        private List<PipelineFilterMessages.PartialUpdateMessage> pendingPartialUpdates = new List<PipelineFilterMessages.PartialUpdateMessage>();

        public bool IsEmpty => pendingWorkers.Count == 0;

        public bool HasPendingSettingsChange => PendingSettingsChange != null;
        
        public DownscalingMessages.SetDownscalingSettingsMessage PendingSettingsChange { get; private set; }
        public IEnumerable<PipelineFilterMessages.PartialUpdateMessage> PendingPartialUpdates => pendingPartialUpdates;

        public void AddWork(IActorRef worker)
        {
            pendingWorkers.Add(worker);
        }

        public void WorkCompleted(IActorRef worker)
        {
            pendingWorkers.Remove(worker);
        }

        public void AddPendingSettingsChange(DownscalingMessages.SetDownscalingSettingsMessage message)
        {
            PendingSettingsChange = message;
            // delete incompatible partial updates
        }

        public void AddPendingPartialUpdate(PipelineFilterMessages.PartialUpdateMessage message)
        {
            pendingPartialUpdates.Add(message);
        }

        internal void ClearPendingWork()
        {
            PendingSettingsChange = null;
            pendingPartialUpdates.Clear();
        }
    }
}
