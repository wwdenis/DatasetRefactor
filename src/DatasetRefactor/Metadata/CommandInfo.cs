using System;

namespace DatasetRefactor.Metadata
{
    internal class CommandInfo : IEquatable<CommandInfo>
    {
        public ActionType Type { get; set; }

        public string Name { get; set; }

        public string Text { get; set; }

        public override int GetHashCode() => (Type, Name, Text).GetHashCode();

        public override bool Equals(object obj) => Equals(obj as CommandInfo);
        
        public bool Equals(CommandInfo other)
        {
            return other is not null
                && this.GetHashCode() == other.GetHashCode();
        }
    }
}
