using System;
using System.Collections.Generic;

namespace DatasetRefactor.Entities
{
    internal class ScanFilter
    {
        public ScanFilter()
        {
        }

        public ScanFilter(string datasetName, string adapterName, IEnumerable<string> actions)
        {
            this.DatasetName = datasetName;
            this.AdapterName = adapterName;
            this.Actions = actions;
        }

        public string DatasetName { get; set; }

        public string AdapterName { get; set; }

        public IEnumerable<string> Actions { get; set; }
    }
}
