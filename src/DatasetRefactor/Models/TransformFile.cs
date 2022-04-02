namespace DatasetRefactor.Models
{
    using System.Collections.Generic;
    
    public class TransformFile
    {

        public string Name { get; set; }

        public string Directory { get; set; }

        public string Contents { get; set; }

        public object Source { get; set; }
        
        public string Adapter { get; set; }

        public bool IsBase { get; set; }
    }
}
