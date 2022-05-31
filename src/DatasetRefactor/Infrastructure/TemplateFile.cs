namespace DatasetRefactor.Infrastructure
{
    internal class TemplateFile
    {
        public string Name { get; set; }

        public string Path { get; set; }

        public string Contents { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(this.Contents);
        }

        public override string ToString()
        {
            return this.Path;
        }
    }
}
