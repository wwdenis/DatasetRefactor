namespace DatasetRefactor.Metadata
{
    internal class RootInfo
    {
        public RootInfo()
        {
        }

        public RootInfo(string @namespace)
        {
            this.Namespace = @namespace;
        }

        public string Namespace { get; set; }
    }
}
