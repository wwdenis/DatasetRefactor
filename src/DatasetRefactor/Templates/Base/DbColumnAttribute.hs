#+Dataset#namespace #Namespace##+#
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    [AttributeUsage(AttributeTargets.Property)]
    internal class DbColumnAttribute : Attribute
    {
        public DbColumnAttribute()
        {
        }

        public DbColumnAttribute(string columnName)
        {
            this.ColumnName = columnName;
        }

        public bool PrimaryKey { get; set; }

        public string ColumnName { get; set; }

        public string PropertyName { get; set; }

        public Type ColumnType { get; set; }

        public static IEnumerable<DbColumnAttribute> BuildColumns(Type rowType)
        {
            return from i in rowType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                   let attr = i.GetCustomAttribute<DbColumnAttribute>()
                   select new DbColumnAttribute
                   {
                       PropertyName = i.Name,
                       ColumnName = string.IsNullOrWhiteSpace(attr?.ColumnName) ? i.Name : attr?.ColumnName,
                       ColumnType = i.PropertyType,
                       PrimaryKey = attr?.PrimaryKey ?? false
                   };
        }

        public static Dictionary<string, string> BuildNames(Type rowType)
        {
            var columns = BuildColumns(rowType);
            return columns
                .Where(i => !string.Equals(i.ColumnName, i.PropertyName, StringComparison.Ordinal))
                .ToDictionary(k => k.PropertyName, v => v.ColumnName);
        }
    }
}