using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DatasetRefactor.Entities;
using DatasetRefactor.Extensions;

namespace DatasetRefactor.Infrastructure
{
    internal sealed class TypeScanner
    {
        private const string TableManagerTyme = "TableAdapterManager";

        private static readonly string[] DatasetBaseTypes = new[] { "System.Data.DataSet" };
        private static readonly string[] TableBaseTypes = new[] { "System.Data.TypedTableBase`1", "System.Data.DataTable" };
        private static readonly string[] AdapterBaseTypes = new[] { "System.ComponentModel.Component" };

        private readonly IEnumerable<Type> adapters;
        private readonly IEnumerable<Type> datasets;
        private readonly IEnumerable<Type> tables;

        public TypeScanner(Assembly assembly)
        {
            if (assembly is null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            this.adapters = assembly.FindTypes(AdapterBaseTypes, TableManagerTyme);
            this.datasets = assembly.FindTypes(DatasetBaseTypes);
            this.tables = assembly.FindTypes(TableBaseTypes);
        }

        public TypeResult Scan(IEnumerable<ScanFilter> filters = null)
        {
            var list = new List<TypeMetadata>();
            var errors = new List<string>();
            
            foreach (var adapter in this.adapters)
            {
                ScanFilter filter = null;

                if (filters?.Any() ?? false)
                {
                    filter = filters.FirstOrDefault(i => string.Equals(i.AdapterName, adapter.Name, StringComparison.OrdinalIgnoreCase));
                    if (filter is null)
                    {
                        continue;
                    }
                }

                var success = TryParse(adapter, filter, out var metadata, out var error);

                if (success)
                {
                    list.Add(metadata);
                }
                else
                {
                    errors.Add(error);
                }
            }

            return new TypeResult
            {
                Items = list,
                Errors = errors,
            };
        }

        private bool TryParse(Type adapterType, ScanFilter filter, out TypeMetadata metadata, out string error)
        {
            metadata = null;
            error = string.Empty;

            var regex = new Regex(@"(?<Namespace>.*)\.(?<Dataset>\w*)TableAdapters\.(?<Table>.*)TableAdapter$");
            var match = regex.Match(adapterType.FullName);
            if (!match.Success)
            {
                error = $"Invalid Adapter Name: {adapterType.FullName}";
                return false;
            }

            var root = match.Groups["Namespace"].Value;
            var dataset = match.Groups["Dataset"].Value;
            var table = match.Groups["Table"].Value;

            var datasetName = string.Join(".", root, dataset);
            var tableName = string.Join(string.Empty, table, "DataTable");

            var datasetType = this.datasets.SingleOrDefault(i => i.FullName == datasetName);
            var tableType = datasetType?.GetNestedTypes().SingleOrDefault(i => i.Name == tableName && this.tables.Contains(i));

            metadata = new TypeMetadata
            {
                AdapterType = adapterType,
                DatasetType = datasetType,
                TableType = tableType,
                AdapterName = adapterType.Name,
                DatasetName = datasetName,
                TableName = tableName,
                SelectedActions = filter?.Actions,
            };

            return true;
        }
    }
}
