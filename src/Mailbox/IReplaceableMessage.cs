namespace ActorPipeline.Mailbox
{
    public interface IReplaceableMessage
    {
        bool CanReplace(IReplaceableMessage other);
    }
}
