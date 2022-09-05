namespace ActorPipeline.Messages.Data
{
    public class FilterPropertiesBuilder
    {
        private readonly string name;
        private bool isSourceFilter;

        public static FilterPropertiesBuilder WithName(string name) => new FilterPropertiesBuilder(name);

        private FilterPropertiesBuilder(string name)
        {
            this.name = name;
            isSourceFilter = false;
        }

        public FilterPropertiesBuilder AsSourceFilter()
        {
            isSourceFilter = true;
            return this;
        }

        public FilterProperties Build() => new FilterProperties(name, isSourceFilter);        
    }
}
