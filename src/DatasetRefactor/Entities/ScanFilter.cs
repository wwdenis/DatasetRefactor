using System.Collections.Generic;

namespace DatasetRefactor.Entities
{
    public class ScanFilter
    {
        public ScanFilter()
        {
        }

        public ScanFilter(string adapterName, IEnumerable<string> actions)
        {
            this.AdapterName = adapterName;
            this.Actions = actions;
        }

        public string AdapterName { get; set; }

        public IEnumerable<string> Actions { get; set; }
    }
}
