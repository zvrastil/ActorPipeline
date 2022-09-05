namespace ActorPipeline.Messages.Data
{
    public class FilterProperties
    {
        internal FilterProperties(string name, bool isSourceFilter)
        {
            Name = name;
            IsSourceFilter = isSourceFilter;
        }

        public string Name { get; }
        public bool IsSourceFilter { get; }
    }
}
