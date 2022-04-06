namespace DatasetRefactor.Models
{
    using System.Collections.Generic;
    
    public class TransformFile
    {

        public string Name { get; set; }

        public string Directory { get; set; }

        public string Contents { get; set; }

        public TableGroup Source { get; set; }
        
        public string Adapter { get; set; }
    }
}
