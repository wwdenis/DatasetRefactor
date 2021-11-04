using System;

namespace DatasetRefactor.Models
{
    public class TableCommand : IEquatable<TableCommand>
    {
        public TableActionType Type { get; set; }

        public string Name { get; set; }

        public string Text { get; set; }

        public override int GetHashCode() => (Type, Name, Text).GetHashCode();

        public override bool Equals(object obj) => Equals(obj as TableCommand);
        
        public bool Equals(TableCommand other)
        {
            return other is not null
                && this.GetHashCode() == other.GetHashCode();
        }
    }
}
