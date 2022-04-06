using System.Collections.Generic;

namespace DatasetRefactor.Models
{
    public class TableFilter
    {
        public TableFilter()
        {
        }

        public TableFilter(string name, IEnumerable<string> actions)
        {
            this.Name = name;
            this.Actions = actions;
        }

        public string Name { get; set; }

        public IEnumerable<string> Actions { get; set; }
    }
}
