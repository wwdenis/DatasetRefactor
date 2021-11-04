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

        public string Name { get; set; }

        public string DatasetName { get; set; }

        public string LocalNamespace { get; set; }

        public string RootNamespace { get; set; }

        public IEnumerable<TableAction> AdapterActions { get; set; }
    }
}
