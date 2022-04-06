using System;
using System.Collections.Generic;
using System.Linq;

namespace DatasetRefactor.Metadata
{
    public class ActionInfo
    {
        public ActionInfo()
        {
            this.Type = ActionType.None;
            this.Parameters = Enumerable.Empty<ActionParameter>();
        }

        public string Table { get; set; }

        public string Command { get; set; }

        public ActionType Type { get; set; }

        public string Name { get; set; }

        public string Prefix { get; set; }

        public string Suffix { get; set; }

        public string ReturnType { get; set; }

        public IEnumerable<ActionParameter> Parameters { get; set; }
    }
}
