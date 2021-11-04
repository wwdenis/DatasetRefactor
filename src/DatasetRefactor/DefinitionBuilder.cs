using System;
using System.Collections.Generic;
using System.Data;
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

        private readonly string assemblyPath;

        public DefinitionBuilder(string assemblyPath)
        {
            this.assemblyPath = assemblyPath;
        }

        public IEnumerable<TableMetadata> Build(string tableName = null)
        {

            var assembly = Assembly.LoadFrom(this.assemblyPath);
            var types = assembly.FindTypes(TableAdapterBaseType);

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
            var methods = type.GetDeclaredMethods();
            var actions = new List<TableAction>();

            foreach (var method in methods)
            {
                var action = ParseAction(method);
                actions.Add(action);
            }

            var match = Regex.Match(type.Namespace, @"(?<RootNamespace>.*)\.(?<DatasetName>\w*)TableAdapters$");

            return new TableMetadata
            {
                Name = type.Name,
                LocalNamespace = type.Namespace,
                RootNamespace = match.Groups["RootNamespace"].Value,
                DatasetName = match.Groups["DatasetName"].Value,
                AdapterActions = actions,
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
    }
}
