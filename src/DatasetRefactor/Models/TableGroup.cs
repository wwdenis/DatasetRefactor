namespace DatasetRefactor.Models
{
    public class TableGroup
    {
        public DatasetInfo Dataset { get; set; }

        public TableInfo Table { get; set; }

        public AdapterInfo Adapter { get; set; }
    }
}
