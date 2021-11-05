using System.Collections.Generic;
using System.Linq;

namespace DatasetRefactor.Models
{
    public class AdapterInfo
    {
        public AdapterInfo()
        {
            this.Actions = Enumerable.Empty<ActionInfo>();
            this.Commands = Enumerable.Empty<CommandInfo>();
        }

        public string Name { get; set; }

        public string Namespace { get; set; }

        public IEnumerable<ActionInfo> Actions { get; set; }
        
        public IEnumerable<CommandInfo> Commands { get; internal set; }
    }
}
