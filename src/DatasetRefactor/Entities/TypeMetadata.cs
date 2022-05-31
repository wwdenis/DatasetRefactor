using System;
using System.Collections.Generic;

namespace DatasetRefactor.Entities
{
    internal class TypeMetadata
    {
        public Type AdapterType { get; set; }

        public Type DatasetType { get; set; }

        public Type TableType { get; set; }

        public string AdapterName { get; set; }

        public string DatasetName { get; set; }

        public string TableName { get; set; }

        public IEnumerable<string> SelectedActions { get; set; }
    }
}
