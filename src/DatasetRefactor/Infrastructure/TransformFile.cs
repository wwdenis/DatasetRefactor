namespace DatasetRefactor.Infrastructure
{
    public class TransformFile
    {
        public string Name { get; set; }

        public string Directory { get; set; }

        public string Contents { get; set; }

        public string SourceName { get; set; }
        
        public object SourceData { get; set; }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
