using System.Collections.Generic;
using System.Linq;

namespace DatasetRefactor.Models
{
    public class TableAction
    {
        public TableAction()
        {
            this.Type = TableActionType.None;
            this.Parameters = Enumerable.Empty<TableActionParameter>();
        }

        public string Name { get; set; }

        public TableActionType Type { get; set; }

        public string Suffix { get; internal set; }

        public IEnumerable<TableActionParameter> Parameters { get; set; }
    }
}
