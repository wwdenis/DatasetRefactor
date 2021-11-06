using System.Collections.Generic;
using System.Linq;

namespace DatasetRefactor.Models
{
    public class TableInfo
    {
        public TableInfo()
        {
            this.Columns = Enumerable.Empty<ColumnInfo>();
        }

        public string Name { get; set; }

        public string Namespace { get; set; }

        public IEnumerable<ColumnInfo> Columns { get; internal set; }
    }
}
