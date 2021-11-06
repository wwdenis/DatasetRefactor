using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using DatasetRefactor.Extensions;
using DatasetRefactor.Models;

namespace DatasetRefactor
{
    public class DefinitionBuilder
    {
        const string DatasetBaseType = "System.Data.DataSet";
        const string TableBaseType = "System.Data.TypedTableBase`1";
        const string AdapterBaseType = "System.ComponentModel.Component";

        private readonly string assemblyPath;

        public DefinitionBuilder(string assemblyPath)
        {
            this.assemblyPath = assemblyPath;
        }

        public IEnumerable<TableGroup> Build(string datasetName = null)
        {
            var assembly = Assembly.LoadFrom(this.assemblyPath);
            var datasets = assembly.FindTypes(DatasetBaseType);
            var adapters = assembly.FindTypes(AdapterBaseType);

            var result = new List<TableGroup>();

            if (!string.IsNullOrWhiteSpace(datasetName))
            {
                datasets = datasets.Where(i => datasetName.Equals(i.Name));
            }

            foreach (var datasetType in datasets)
            {
                var datasetInfo = BuildDataset(datasetType);
                var tables = datasetType.FindTypes(TableBaseType);

                foreach (var tableType in tables)
                {
                    var tableInfo = BuildTable(tableType);

                    var adapterName = $"{tableInfo.Name}TableAdapter";
                    var adapterType = adapters.Single(i => i.Name.Equals(adapterName));
                    var adapterInfo = BuildAdapter(adapterType);

                    var tableGroup = new TableGroup
                    {
                        Dataset = datasetInfo,
                        Table = tableInfo,
                        Adapter = adapterInfo,
                    };
                    
                    result.Add(tableGroup);
                }
            }

            return result;
        }

        private static DatasetInfo BuildDataset(Type type)
        {
            return new DatasetInfo
            {
                Name = type.Name,
                Namespace = type.Namespace,
            };
        }

        private static TableInfo BuildTable(Type type)
        {
            var tableName = type.Name.Replace("DataTable", string.Empty);
            var datasetNamespace = type.FullName.Split('+').First();
            var columns = BuildColumns(type);

            if (!columns.Any())
            {
                return null;
            }

            return new TableInfo
            {
                Name = tableName,
                Namespace = datasetNamespace,
                Columns = columns,
            };
        }

        private static IEnumerable<ColumnInfo> BuildColumns(Type tableType)
        {
            var instance = Activator.CreateInstance(tableType);
            var sourceColumns = instance.GetPropertyValue<DataColumnCollection>("Columns");
            var keyColumns = instance.GetPropertyValue<DataColumn[]>("PrimaryKey");

            var columns = new List<ColumnInfo>();

            foreach (DataColumn col in sourceColumns)
            {
                var propertyName = col.ColumnName.Contains(" ") ? col.ColumnName.Replace(" ", "_") : string.Empty;

                var column = new ColumnInfo
                {
                    Name = col.ColumnName,
                    Type = col.DataType.GetFriendlyName(),
                    Property = propertyName,
                    IsKey = keyColumns.Contains(col),
                };

                columns.Add(column);
            }

            return columns;
        }

        private static AdapterInfo BuildAdapter(Type type)
        {
            var adapter = InitAdapter(type);
            var methods = type.GetDeclaredMethods();
            
            var actions = new List<ActionInfo>();
            var commands = new HashSet<CommandInfo>();

            foreach (var method in methods)
            {
                var action = ParseAction(method);
                actions.Add(action);

                adapter.InvokeDefault(method);
                var command = ParseCommand(action, adapter);
                commands.Add(command);
            }

            return new AdapterInfo
            {
                Name = type.Name,
                Namespace = type.Namespace,
                Actions = actions,
                Commands = commands,
            };
        }

        private static ActionInfo ParseAction(MethodInfo method)
        {
            var parameters = new List<ActionParameter>();

            foreach (var parameter in method.GetParameters())
            {
                var actionParameter = ParseParameter(parameter);
                parameters.Add(actionParameter);
            }

            var (type, suffix) = ParseActionType(method);

            return new ActionInfo
            {
                Name = method.Name,
                Type = type,
                Suffix = suffix,
                Parameters = parameters,
            };
        }

        private static ActionParameter ParseParameter(ParameterInfo parameter)
        {
            return new ActionParameter
            {
                Name = parameter.Name,
                Type = parameter.ParameterType.GetFriendlyName(),
            };
        }

        private static CommandInfo ParseCommand(ActionInfo action, object instance)
        {
            var sqlAdapter = instance.GetPropertyValue<SqlDataAdapter>("Adapter");

            var command = action.Type switch
            {
                ActionType.Select => sqlAdapter.SelectCommand,
                ActionType.Insert => sqlAdapter.InsertCommand,
                ActionType.Delete => sqlAdapter.DeleteCommand,
                ActionType.Update => sqlAdapter.UpdateCommand,
                _ => null,
            };

            return new CommandInfo
            {
                Type = action.Type,
                Name = action.Suffix,
                Text = command?.CommandText ?? string.Empty,
            };
        }

        private static (ActionType, string) ParseActionType(MethodInfo method)
        {
            if (Enum.TryParse<ActionType>(method.Name, out var actionType))
            {
                return (actionType, string.Empty);
            }

            var selectPrefixes = new[] { "Fill", "GetData" };

            foreach (var prefix in selectPrefixes)
            {
                if (method.Name.HasSuffix(prefix, out var suffix))
                {
                    return (ActionType.Select, suffix);
                }
            }

            return (ActionType.Scalar, string.Empty);
        }

        private static object InitAdapter(Type type)
        {
            var instance = Activator.CreateInstance(type);
            instance.InvokeDefault("InitCommandCollection");
            
            var sqlAdapter = instance.GetPropertyValue<SqlDataAdapter>("Adapter");
            var selectCommands = instance.GetPropertyValue<IDbCommand[]>("CommandCollection");
            var updateCommands = new[]
            {
                sqlAdapter.UpdateCommand,
                sqlAdapter.InsertCommand,
                sqlAdapter.DeleteCommand,
            };

            var allCommands = selectCommands.Union(updateCommands);

            foreach (var cmd in allCommands)
            {
                cmd.Connection = null;
            }

            return instance;
        }
    }
}
