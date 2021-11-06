using System.Collections.Generic;
using System.Linq;

namespace DatasetRefactor.Models
{
    public class TableInfo
    {
        public TableInfo()
        {
            this.Columns = Enumerable.Empty<ColumnInfo>();
            this.Actions = Enumerable.Empty<ActionInfo>();
        }

        public string Name { get; set; }

        public string Namespace { get; set; }

        public IEnumerable<ColumnInfo> Columns { get; set; }

        public IEnumerable<ActionInfo> Actions { get; set; }
    }
}
