#+Dataset#namespace #Namespace##+#
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;

    public abstract class DbRow : DataRow
    {
        private static readonly Regex IsNullRegex = new Regex("^Is(?<Column>.*)Null$");
        private static readonly Regex SetNullRegex = new Regex("^Set(?<Column>.*)Null$");

        private readonly Dictionary<string, string> NameMappings = new Dictionary<string, string>();

        protected internal DbRow(DataRowBuilder builder) : base(builder)
        {
            this.NameMappings = DbColumnAttribute.BuildNames(this.GetType());
        }

        protected T Get<T>([CallerMemberName] string propertyName = null)
        {
            var columnName = ResolveName(propertyName);
            return this.IsNull(columnName) ? default : (T)this[columnName];
        }

        protected void Set<T>(T value, [CallerMemberName] string propertyName = null)
        {
            var columnName = ResolveName(propertyName);
            this[columnName] = value;
        }

        protected bool HasNull([CallerMemberName] string methodName = null)
        {
            var columnName = ParseName(methodName, IsNullRegex);
            return this.IsNull(columnName);
        }

        protected void SetNull([CallerMemberName] string methodName = null)
        {
            var columnName = ParseName(methodName, SetNullRegex);
            this[columnName] = DBNull.Value;
        }

        private string ParseName(string methodNane, Regex regex)
        {
            var match = regex.Match(methodNane);
            var propertyName = match.Success ? match.Groups[1].Value : methodNane;
            return this.ResolveName(propertyName);
        }

        private string ResolveName(string name)
        {
            return this.NameMappings.ContainsKey(name) ? this.NameMappings[name] : name;
        }
    }
}