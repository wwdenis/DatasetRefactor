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

        public ActionType Type { get; set; }

        public string Name { get; set; }

        public string Suffix { get; set; }

        public string ReturnType { get; set; }

        public IEnumerable<ActionParameter> Parameters { get; set; }
    }
}
