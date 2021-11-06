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
        const string TableBaseType = "System.Data.TypedTableBase";
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

            var result = new List<TableGroup>();

            if (!string.IsNullOrWhiteSpace(datasetName))
            {
                datasets = datasets.Where(i => datasetName.Equals(i.Name));
            }

            foreach (var dataset in datasets)
            {
                var datasetInfo = BuildDataset(dataset);
                var tables = dataset.GetNestedTypes().Where(i => i.BaseType.FullName.StartsWith(TableBaseType));

                foreach (var table in tables)
                {
                    var adapter = assembly.FindTypes(AdapterBaseType).FirstOrDefault(i => i.Name.Equals(table.Name.Replace("DataTable", "TableAdapter")));

                    var tableInfo = BuildTable(table);
                    var adapterInfo = BuildAdapter(adapter);

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
            return new TableInfo
            {
                Name = type.Name,
                Namespace = type.Namespace,
            };
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
            var parameters = new List<Models.ParameterInfo>();

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

        private static Models.ParameterInfo ParseParameter(System.Reflection.ParameterInfo parameter)
        {
            return new Models.ParameterInfo
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
