using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using DatasetRefactor.Extensions;
using DatasetRefactor.Infrastructure;
using DatasetRefactor.Models;

namespace DatasetRefactor
{
    public class TableGroupBuilder
    {
        public event EventHandler<string> Progress;

        public IEnumerable<TableGroup> Build(IEnumerable<TypeMetadata> metadata)
        {
            this.OnProgress($"Starting Reading Datasets");

            var result = new List<TableGroup>();

            foreach (var item in metadata)
            {
                this.OnProgress(item.AdapterName);

                var adapterInfo = BuildAdapter(item);
                var tableInfo = BuildTable(item);
                var datasetInfo = new DatasetInfo(item.DatasetType);

                var tableGroup = new TableGroup
                {
                    Dataset = datasetInfo,
                    Table = tableInfo,
                    Adapter = adapterInfo,
                };

                result.Add(tableGroup);
            }

            this.OnProgress($"Finished Reading Datasets");

            return result;
        }

        private static TableInfo BuildTable(TypeMetadata meta)
        {
            var type = meta.TableType;
            if (type is null)
            {
                return null;
            }

            var rowName = type.Name.Replace("DataTable", "Row");
            var tableName = type.Name.Replace("DataTable", string.Empty);
            var datasetNamespace = type.FullName.Split('+').First();

            var columns = BuildColumns(type);
            var actions = new List<ActionInfo>();
            var methods = type
                .GetDeclaredMethods()
                .Where(i => i.ReturnType.Name.Equals(rowName));

            foreach (var method in methods)
            {
                var action = BuildAction(method);
                if (action?.Type == ActionType.Find)
                {
                    action.Table = tableName;
                    actions.Add(action);
                }
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

        private static AdapterInfo BuildAdapter(TypeMetadata meta)
        {
            var type = meta.AdapterType;
            var selected = meta.SelectedActions ?? Enumerable.Empty<string>();

            using var manager = SqlManager.Create(type);
            
            var actions = new HashSet<ActionInfo>();
            var commands = new HashSet<CommandInfo>();

            var methods = from i in type.GetDeclaredMethods()
                          let parameters = i.GetParameters()
                          where parameters.All(p => p.ParameterType.IsSimple())
                          && (selected.Any() || selected.Contains(i.Name))
                          select i;

            foreach (var method in methods)
            {
                manager.CallMethod(method);

                var action = BuildAction(method, manager);
                var command = BuildCommand(method, manager, action);

                actions.Add(action);
                commands.Add(command);
            }

            return new AdapterInfo
            {
                Name = type.Name,
                Namespace = type.Namespace,
                Scalar = actions.Where(i => i.Type == ActionType.Execute),
                Select = actions.Where(i => i.Type == ActionType.Select),
                Insert = actions.FirstOrDefault(i => i.Type == ActionType.Insert),
                Delete = actions.FirstOrDefault(i => i.Type == ActionType.Delete),
                Update = actions.FirstOrDefault(i => i.Type == ActionType.Update),
                Commands = commands,
            };
        }

        private static ActionInfo BuildAction(MethodInfo method, SqlManager manager = null)
        {
            var isSelect = manager?.Adapter?.SelectCommand != null;
            var tableName = manager?.TableName;
            var actionName = method.Name;
            var actionType = ActionType.None;

            var isSpecial = Enum.TryParse(actionName, out ActionType specialType);
            var isFind = actionName.HasSuffix("Find", out _);

            if (isSelect)
            {
                actionType = ActionType.Select;
            }
            else if(isFind)
            {
                actionType = ActionType.Find;
            }
            else if (isSpecial)
            {
                actionType = specialType;
            }
            else
            {
                actionType = ActionType.Execute;
            }

            SplitAction(actionName, out var prefix, out var suffix);

            return new ActionInfo
            {
                Type = actionType,
                Prefix = prefix,
                Suffix = suffix,
                Name = actionName,
                Table = tableName,
                Command = BuildCommandName(actionType, actionName),
                ReturnType = method.ReturnType.GetCsName(),
                Parameters = method.GetParameters().Select(i => new ActionParameter(i)),
            };
        }

        private static CommandInfo BuildCommand(MethodInfo method, SqlManager manager, ActionInfo action)
        {
            var adapter = manager.Adapter;

            var command = action.Type switch
            {
                ActionType.Select => adapter.SelectCommand,
                ActionType.Insert => adapter.InsertCommand,
                ActionType.Delete => adapter.DeleteCommand,
                ActionType.Update => adapter.UpdateCommand,
                _ => manager.GetLastCalled(),
            };

            return new CommandInfo
            {
                Name = BuildCommandName(action.Type, method.Name),
                Type = action.Type,
                Text = command?.CommandText ?? string.Empty,
            };
        }

        private static string BuildCommandName(ActionType actionType, string actionName)
        {
            SplitAction(actionName, out _, out var suffix);

            if (!string.IsNullOrWhiteSpace(suffix))
            {
                suffix = "_" + suffix;
            }

            return actionType switch
            {
                ActionType.Select => $"Select{suffix}",
                ActionType.Execute => $"Execute_{actionName}",
                ActionType.Insert or ActionType.Delete or ActionType.Update => $"{actionType}",
                _ => actionName,
            };
        }

        private static void SplitAction(string actionName, out string actionPrefix, out string actionSuffix)
        {
            var prefixes = new[] { "Fill", "GetData", "Get", "Find" };

            actionPrefix = string.Empty;
            actionSuffix = string.Empty;

            foreach (var prefix in prefixes)
            {
                if (actionName.HasSuffix(prefix, out var suffix))
                {
                    actionPrefix = prefix;
                    actionSuffix = suffix;
                    break;
                }
            }
        }

        private void OnProgress(string message)
        {
            this.Progress?.Invoke(this, message);
        }
    }
}
