using System.Collections.Generic;
using System.Linq;

namespace DatasetRefactor.Entities
{
    internal class TypeResult
    {
        public TypeResult()
        {
            this.Items = Enumerable.Empty<TypeMetadata>();
            this.Errors = Enumerable.Empty<string>();
        }

        public IEnumerable<TypeMetadata> Items { get; set; }

        public IEnumerable<string> Errors { get; set; }
    }
}
