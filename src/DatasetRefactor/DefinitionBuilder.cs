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

        public IEnumerable<TableMetadata> Build(string tableName = null)
        {
            var assembly = Assembly.LoadFrom(this.assemblyPath);
            var types = assembly.FindTypes(TableAdapterBaseType, "TableAdapterManager");

            var result = new List<TableMetadata>();

            if (!string.IsNullOrWhiteSpace(tableName))
            {
                types = types.Where(i => i.Name.StartsWith(tableName));
            }

            foreach (var type in types)
            {
                var adapter = BuildAdapter(type);
                result.Add(adapter);
            }

            return result;
        }

        private static TableMetadata BuildAdapter(Type type)
        {
            var adapter = InitAdapter(type);
            var methods = type.GetDeclaredMethods();
            
            var actions = new List<TableAction>();
            var commands = new HashSet<TableCommand>();

            foreach (var method in methods)
            {
                var action = ParseAction(method);
                actions.Add(action);

                adapter.InvokeDefault(method);
                var command = ParseCommand(action, adapter);
                commands.Add(command);
            }

            var header = ParseHeader(type);

            return new TableMetadata
            {
                RootNamespace = header[RootNamespace],
                DatasetName = header[DatasetName],
                TableName = header[TableName],
                AdapterNamespace = type.Namespace,
                AdapterActions = actions,
                SqlCommands = commands,
            };
        }

        private static TableAction ParseAction(MethodInfo method)
        {
            var parameters = new List<TableActionParameter>();

            foreach (var parameter in method.GetParameters())
            {
                var actionParameter = ParseParameter(parameter);
                parameters.Add(actionParameter);
            }

            var (type, suffix) = ParseActionType(method);

            return new TableAction
            {
                Name = method.Name,
                Type = type,
                Suffix = suffix,
                Parameters = parameters,
            };
        }

        private static TableActionParameter ParseParameter(ParameterInfo parameter)
        {
            return new TableActionParameter
            {
                Name = parameter.Name,
                Type = parameter.ParameterType.GetFriendlyName(),
            };
        }

        private static TableCommand ParseCommand(TableAction action, object instance)
        {
            var sqlAdapter = instance.GetPropertyValue<SqlDataAdapter>("Adapter");

            var command = action.Type switch
            {
                TableActionType.Fill => sqlAdapter.SelectCommand,
                TableActionType.GetData => sqlAdapter.SelectCommand,
                TableActionType.Insert => sqlAdapter.InsertCommand,
                TableActionType.Delete => sqlAdapter.DeleteCommand,
                TableActionType.Update => sqlAdapter.UpdateCommand,
                _ => null,
            };

            return new TableCommand
            {
                Type = action.Type,
                Name = action.Suffix,
                Text = command?.CommandText ?? string.Empty,
            };
        }

        private static (TableActionType, string) ParseActionType(MethodInfo method)
        {
            if (Enum.TryParse<TableActionType>(method.Name, out var actionType))
            {
                return (actionType, string.Empty);
            }

            var selectTypes = new[] { TableActionType.Fill, TableActionType.GetData };

            foreach (var selectType in selectTypes)
            {
                var suffix = method.Name.GetSuffix(selectType.ToString());
                if (!string.IsNullOrWhiteSpace(suffix))
                {
                    return (selectType, suffix);
                }
            }

            return (TableActionType.Scalar, string.Empty);
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
