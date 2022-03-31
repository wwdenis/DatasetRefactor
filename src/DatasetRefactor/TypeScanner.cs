using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DatasetRefactor.Extensions;
using DatasetRefactor.Models;

namespace DatasetRefactor
{
    public class TypeScanner
    {
        private static readonly string[] DatasetBaseTypes = new[] { "System.Data.DataSet" };
        private static readonly string[] TableBaseTypes = new[] { "System.Data.TypedTableBase`1", "System.Data.DataTable" };
        private static readonly string[] AdapterBaseTypes = new[] { "System.ComponentModel.Component" };
        private readonly Assembly assembly;

        public TypeScanner(Assembly assembly)
        {
            this.assembly = assembly;
        }

        public IEnumerable<TypeMetadata> Scan(string includeTable = null)
        {
            var adapters = this.assembly.FindTypes(AdapterBaseTypes);
            var datasets = this.assembly.FindTypes(DatasetBaseTypes);
            var tables = this.assembly.FindTypes(TableBaseTypes);

            var result = new List<TypeMetadata>();

            foreach (var adapterType in adapters)
            {
                var found = FindEntities(adapterType, out var datasetName, out var tableName);
                if (!found)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(tableName) && !string.IsNullOrWhiteSpace(includeTable) && !tableName.Contains(includeTable))
                {
                    continue;
                }

                var datasetType = datasets.SingleOrDefault(i => i.FullName == datasetName);
                var tableType = datasetType?.GetNestedTypes().SingleOrDefault(i => i.Name == tableName);

                if (datasetType is not null)
                {
                    var search = new TypeMetadata
                    {
                        AdapterType = adapterType,
                        DatasetType = datasetType,
                        TableType = tableType,
                        AdapterName = adapterType.Name,
                        DatasetName = datasetName,
                        TableName = tableName,
                    };

                    result.Add(search);
                }
            }

            return result;
        }

        public static bool FindEntities(Type adapterType, out string datasetName, out string tableName)
        {
            datasetName = string.Empty;
            tableName = string.Empty;

            var regex = new Regex(@"(?<Namespace>.*)\.(?<Dataset>\w*)TableAdapters\.(?<Table>.*)TableAdapter$");
            var match = regex.Match(adapterType.FullName);

            if (!match.Success)
            {
                return false;
            }

            var root = match.Groups["Namespace"].Value;
            var dataset = match.Groups["Dataset"].Value;
            var table = match.Groups["Table"].Value;

            datasetName = string.Join(".", root, dataset);
            tableName = string.Join(string.Empty, table, "DataTable");

            return true;
        }
    }
}
