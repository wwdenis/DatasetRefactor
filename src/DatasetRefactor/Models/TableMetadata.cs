using System.Collections.Generic;
using System.Linq;

namespace DatasetRefactor.Models
{
    public class TableMetadata
    {
        public TableMetadata()
        {
            this.AdapterActions = Enumerable.Empty<TableAction>();
        }

        public string TableName { get; set; }

        public string DatasetName { get; set; }

        public string AdapterNamespace { get; set; }

        public string RootNamespace { get; set; }

        public IEnumerable<TableAction> AdapterActions { get; set; }
    }
}
