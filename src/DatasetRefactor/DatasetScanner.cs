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
    public class DatasetScanner
    {
        const string DatasetBaseType = "System.Data.DataSet";
        const string TableBaseType = "System.Data.TypedTableBase`1";
        const string AdapterBaseType = "System.ComponentModel.Component";

        private readonly Assembly assembly;

        public DatasetScanner(Assembly assembly)
        {
            this.assembly = assembly;
        }

        public IEnumerable<TableGroup> Scan(string tableName = null)
        {
            var datasets = this.assembly.FindTypes(DatasetBaseType);
            var adapters = this.assembly.FindTypes(AdapterBaseType);

            var result = new List<TableGroup>();

            if (!string.IsNullOrWhiteSpace(tableName))
            {
                datasets = from i in datasets
                           let tables = i.FindTypes(TableBaseType, tableName)
                           where tables.Any()
                           select i;
            }

            foreach (var datasetType in datasets)
            {
                var datasetInfo = BuildDataset(datasetType);
                var tables = datasetType.FindTypes(TableBaseType, tableName);

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
            var rowName = type.Name.Replace("DataTable", "Row");
            var tableName = type.Name.Replace("DataTable", string.Empty);
            var datasetNamespace = type.FullName.Split('+').First();

            var actions = new List<ActionInfo>();
            var methods = type
                .GetDeclaredMethods()
                .Where(i => i.ReturnType.Name.Equals(rowName));

            foreach (var method in methods)
            {
                var action = ParseAction(method);
                if (action?.Type == ActionType.Find)
                {
                    action.Table = tableName;
                    actions.Add(action);
                }
            }

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
                Actions = actions,
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
                    Type = col.DataType.GetCsName(),
                    Property = propertyName,
                    IsKey = keyColumns.Contains(col),
                };

                columns.Add(column);
            }

            return columns;
        }

        private static AdapterInfo BuildAdapter(Type type)
        {
            var tableName = type.Name.Replace("TableAdapter", string.Empty);
            var adapter = InitAdapter(type);
            var methods = type.GetDeclaredMethods();
            
            var actions = new HashSet<ActionInfo>();
            var commands = new HashSet<CommandInfo>();

            foreach (var method in methods)
            {
                var action = ParseAction(method);

                if (action is not null)
                {
                    action.Table = tableName;
                    actions.Add(action);
                    
                    adapter.InvokeDefault(method);
                    var command = ParseCommand(action, adapter);
                    commands.Add(command);
                }
            }

            return new AdapterInfo
            {
                Name = type.Name,
                Namespace = type.Namespace,
                Select = actions.Where(i => i.Type == ActionType.Select),
                Insert = actions.FirstOrDefault(i => i.Type == ActionType.Insert),
                Delete = actions.FirstOrDefault(i => i.Type == ActionType.Delete),
                Update = actions.FirstOrDefault(i => i.Type == ActionType.Update),
                Commands = commands,
            };
        }

        private static ActionInfo ParseAction(MethodInfo method)
        {
            var parameters = new List<ActionParameter>();

            foreach (var parameter in method.GetParameters())
            {
                if (!parameter.ParameterType.IsSimple())
                {
                    return null;
                }

                var actionParameter = ParseParameter(parameter);
                parameters.Add(actionParameter);
            }

            var (type, suffix) = ParseActionType(method);

            return new ActionInfo
            {
                Type = type,
                Name = method.Name,
                Suffix = suffix,
                ReturnType = method.ReturnType.GetCsName(),
                Parameters = parameters,
            };
        }

        private static ActionParameter ParseParameter(ParameterInfo parameter)
        {
            return new ActionParameter
            {
                Name = parameter.Name,
                Type = parameter.ParameterType.GetCsName(),
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

            var prefixMap = new Dictionary<string, ActionType>
            {
                { "Fill", ActionType.Select },
                { "GetData", ActionType.Select },
                { "Find", ActionType.Find },
            };

            foreach (var prefix in prefixMap)
            {
                if (method.Name.HasSuffix(prefix.Key, out var suffix))
                {
                    return (prefix.Value, suffix);
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
