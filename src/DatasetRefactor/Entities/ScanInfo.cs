using DatasetRefactor.Metadata;

namespace DatasetRefactor.Entities
{
    internal class ScanInfo
    {
        public DatasetInfo Dataset { get; set; }

        public TableInfo Table { get; set; }

        public AdapterInfo Adapter { get; set; }
    }
}
