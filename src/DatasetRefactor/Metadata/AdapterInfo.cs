using System.Collections.Generic;
using System.Linq;

namespace DatasetRefactor.Metadata
{
    internal class AdapterInfo
    {
        public AdapterInfo()
        {
            this.Commands = Enumerable.Empty<CommandInfo>();
            this.Select = Enumerable.Empty<ActionInfo>();
        }

        public string Name { get; set; }

        public string Namespace { get; set; }

        public TableInfo Table { get; set; }

        public IEnumerable<CommandInfo> Commands { get; set; }
        
        public IEnumerable<ActionInfo> Scalar { get; set; }

        public IEnumerable<ActionInfo> Select { get; set; }

        public ActionInfo Insert { get; set; }

        public ActionInfo Delete { get; set; }

        public ActionInfo Update { get; set; }
    }
}
