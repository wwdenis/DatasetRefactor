using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DatasetRefactor.Extensions;
using DatasetRefactor.Models;

namespace DatasetRefactor
{
    public class DefinitionBuilder
    {
        const string TableAdapterBaseType = "System.ComponentModel.Component";
        const string RootNamespace = "RootNamespace";
        const string DatasetName = "DatasetName";
        const string TableName = "TableName";

        private readonly string assemblyPath;

        public DefinitionBuilder(string assemblyPath)
        {
            this.assemblyPath = assemblyPath;
        }

        public IEnumerable<TableGroup> Build(string tableName = null)
        {
            var assembly = Assembly.LoadFrom(this.assemblyPath);
            var types = assembly.FindTypes(TableAdapterBaseType, "TableAdapterManager");

            var result = new List<TableGroup>();

            if (!string.IsNullOrWhiteSpace(tableName))
            {
                types = types.Where(i => i.Name.StartsWith(tableName));
            }

            foreach (var type in types)
            {
                var item = BuildGroup(type);
                result.Add(item);
            }

            return result;
        }

        private static TableGroup BuildGroup(Type type)
        {
            var dataset = BuildDataset(type);
            var table = BuildTable(type);
            var adapter = BuildAdapter(type);

            return new TableGroup
            {
                Dataset = dataset,
                Table = table,
                Adapter = adapter,
            };
        }

        private static DatasetInfo BuildDataset(Type type)
        {
            var header = ParseHeader(type);

            return new DatasetInfo
            {
                Name = header[DatasetName],
                Namespace = header[RootNamespace],
            };
        }

        private static TableInfo BuildTable(Type type)
        {
            var header = ParseHeader(type);

            return new TableInfo
            {
                Name = header[TableName],
                Namespace = header[RootNamespace] + "." + header[DatasetName],
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
                ActionType.Fill => sqlAdapter.SelectCommand,
                ActionType.GetData => sqlAdapter.SelectCommand,
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

            var selectTypes = new[] { ActionType.Fill, ActionType.GetData };

            foreach (var selectType in selectTypes)
            {
                var suffix = method.Name.GetSuffix(selectType.ToString());
                if (!string.IsNullOrWhiteSpace(suffix))
                {
                    return (selectType, suffix);
                }
            }

            return (ActionType.Scalar, string.Empty);
        }

        private static Dictionary<string, string> ParseHeader(Type type)
        {
            var pattern = new[]
            {
                @"(?<",
                RootNamespace,
                @">.*)\.(?<",
                DatasetName,
                @">\w*)TableAdapters\.(?<",
                TableName,
                @">\w*)TableAdapter$",
            };

            var keys = new[] { RootNamespace, DatasetName, TableName };

            var match = Regex.Match(type.FullName, string.Join(string.Empty, pattern));

            return keys.ToDictionary(k => k, v => match.Groups[v].Value);
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
