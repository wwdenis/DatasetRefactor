using System.Collections.Generic;
using System.Linq;

namespace DatasetRefactor.Models
{
    public class ActionInfo
    {
        public ActionInfo()
        {
            this.Type = ActionType.None;
            this.Parameters = Enumerable.Empty<ActionParameter>();
        }

        public string Name { get; set; }

        public ActionType Type { get; set; }

        public string Suffix { get; internal set; }

        public IEnumerable<ActionParameter> Parameters { get; set; }
    }
}
