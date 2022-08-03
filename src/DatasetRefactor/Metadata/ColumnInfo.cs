namespace DatasetRefactor.Metadata
{
    internal class ColumnInfo
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public string Caption { get; set; }

        public bool IsKey { get; set; }

        public bool IsNull { get; set; }
    }
}
