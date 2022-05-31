using System.Collections.Generic;

namespace DatasetRefactor.Entities
{
    internal class ScanFilter
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
