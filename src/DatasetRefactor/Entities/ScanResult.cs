using System.Collections.Generic;
using System.Linq;
using DatasetRefactor.Metadata;

namespace DatasetRefactor.Entities
{
    internal class ScanResult
    {
        public ScanResult()
        {
            this.Items = Enumerable.Empty<ScanInfo>();
            this.Errors = Enumerable.Empty<string>();
        }

        public RootInfo Root { get; set; }

        public IEnumerable<ScanInfo> Items { get; set; }

        public IEnumerable<string> Errors { get; set; }
    }
}
