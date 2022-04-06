﻿using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using DatasetRefactor.Entities;
using DatasetRefactor.Metadata;
using FluentAssertions;
using HashScript;
using HashScript.Providers;
using Microsoft.CSharp;

namespace DatasetRefactor.Tests.Infrastructure
{
    internal sealed class CodeCompilerFixture : IDisposable
    {
        private readonly string rootNamespace;
        private readonly string datasetName;
        private readonly string outputFile;

        public CodeCompilerFixture(string rootNamespace, string datasetName)
        {
            this.rootNamespace = rootNamespace;
            this.datasetName = datasetName;
            this.outputFile = $"Datasets_{Guid.NewGuid():N}.dll";
        }

        public void Dispose()
        {
            var files = new[]
            {
                this.outputFile,
                Path.ChangeExtension(this.outputFile, "pdb"),
            };

            foreach (var file in files)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
        }

        public Assembly CompileDataset(string tableName, string keyColumn, Dictionary<string, string> columns)
        {
            var sourceCode = BuildDatasetCode(this.rootNamespace, this.datasetName, tableName, keyColumn, columns);
            return TryBuildAssembly(this.rootNamespace, sourceCode);
        }

        static string BuildDatasetCode(string rootNamespace, string datasetName, string tableName, string keyColumn, Dictionary<string, string> columns)
        {
            var datasetInfo = BuildDatasetData(datasetName, tableName, keyColumn, columns);

            var valueProvider = new DictionaryValueProvider(datasetInfo);
            var templateContents = File.ReadAllText(@"Infrastructure\DatasetSchema.hz");
            var templateRenderer = new Renderer(templateContents);
            var schema = templateRenderer.Generate(valueProvider);

            var root = new CodeNamespace(rootNamespace);
            var unit = new CodeCompileUnit();

            using var provider = new CSharpCodeProvider();
            using var writer = new StringWriter();

            TypedDataSetGenerator.Generate(schema, unit, root, provider, TypedDataSetGenerator.GenerateOption.HierarchicalUpdate);
            provider.GenerateCodeFromNamespace(root, writer, null);
            provider.GenerateCodeFromCompileUnit(unit, writer, null);

            var output = writer.ToString();
            return output;
        }

        private Assembly TryBuildAssembly(string rootNamespace, string datasetSource)
        {
            const string SettingsSource =
                @"namespace MyApp.Properties.Settings {
                    internal class Default {
                        public static string MyDatabaseConnectionString = ""Server=.;Database=Test;Trusted_Connection=True;"";
                    }
                }";

            var csc = new CSharpCodeProvider();
            var dependencies = new string[]
            {
                "mscorlib.dll",
                "System.dll",
                "System.Core.dll",
                "System.Data.dll",
                "System.Xml.dll",
            };

            var parameters = new CompilerParameters(dependencies, this.outputFile, true);
            var result = csc.CompileAssemblyFromSource(parameters, datasetSource, SettingsSource);

            if (result.Errors.HasErrors)
            {
                return null;
            }

            return result.CompiledAssembly;
        }

        static Dictionary<string, object> BuildDatasetData(string datasetName, string tableName, string keyColumn, Dictionary<string, string> columns)
        {
            var columnInfo = from i in columns
                             let dbType = i.Value switch
                             {
                                 "int" => "Int",
                                 "string" => "NVarChar",
                                 _ => i.Value,
                             }
                             let colType = i.Value switch
                             {
                                 "int" => "Int32",
                                 "string" => "String",
                                 _ => i.Value,
                             }
                             select new Dictionary<string, object>()
                             {
                                 { "Name", i.Key },
                                 { "Type", colType },
                                 { "DbType", dbType },
                                 { "XsType", i.Value },
                                 { "IsRequired", false },
                                 { "IsKey", i.Key == keyColumn },
                             };

            return new Dictionary<string, object>()
            {
                { "Dataset", datasetName },
                { "Table", tableName },
                { "PrimaryKey", keyColumn },
                { "Select", $"Command: Select" },
                { "Insert", $"Command: Insert" },
                { "Delete", $"Command: Delete" },
                { "Update", $"Command: Update" },
                {  "Columns", columnInfo },
            };
        }

        public ScanResult BuildScanResult(string tableName, string keyColumn, Dictionary<string, string> columns)
        {
            var columnInfo = from i in columns
                             let propertyName = i.Key.Contains(" ") ? i.Key.Replace(" ", "_") : ""
                             select new ColumnInfo
                             {
                                 Name = i.Key,
                                 Type = i.Value,
                                 Property = propertyName,
                                 IsKey = i.Key == keyColumn,
                             };

            var insertParameters = from i in columns
                                   select new ActionParameter
                                   {
                                       Name = i.Key,
                                       Type = i.Value
                                   };

            var deleteParameters = from i in columns
                                   select new ActionParameter
                                   {
                                       Name = $"Original_{i.Key}",
                                       Type = i.Value
                                   };

            var updateParameters = insertParameters.Union(deleteParameters);

            var findParameters = from i in insertParameters
                                 where i.Name == keyColumn
                                 select i;

            var findAction = new ActionInfo
            {
                Name = $"FindBy{keyColumn}",
                ReturnType = $"{tableName}Row",
                Prefix = "Find",
                Suffix = $"By{keyColumn}",
                Table = tableName,
                Command = "FindById",
                Type = ActionType.Find,
                Parameters = findParameters,
            };

            var commandList = new[]
            {
                ActionType.Select,
                ActionType.Insert,
                ActionType.Delete,
                ActionType.Update,
            };

            var adapterCommands = from i in commandList
                                  select new CommandInfo
                                  {
                                      Type = i,
                                      Text = $"Command: {i}",
                                      Name = $"{i}",
                                  };

            var info = new ScanInfo
            {
                Dataset = new DatasetInfo
                {
                    Name = this.datasetName,
                    Namespace = this.rootNamespace,
                },
                Table = new TableInfo
                {
                    Name = tableName,
                    Namespace = string.Join(".", this.rootNamespace, this.datasetName),
                    Columns = columnInfo,
                    Actions = new[] { findAction },
                },
                Adapter = new AdapterInfo
                {
                    Name = tableName + "TableAdapter",
                    Namespace = string.Join("", this.rootNamespace, ".", this.datasetName, "TableAdapters"),
                    Commands = adapterCommands,
                    Insert = BuildAction(ActionType.Insert, "Insert", "", "Insert", "int", tableName, insertParameters),
                    Delete = BuildAction(ActionType.Delete, "Delete", "", "Delete", "int", tableName, deleteParameters),
                    Update = BuildAction(ActionType.Update, "Update", "", "Update", "int", tableName, updateParameters),
                    Select = new[]
                    {
                        BuildAction(ActionType.Select, "GetData", "GetData", "Select", $"{tableName}DataTable", tableName)
                    },
                    Scalar = new ActionInfo[0],
                }
            };

            return new ScanResult
            {
                Root = new RootInfo(this.rootNamespace),
                Items = new[] { info },
                Errors = Enumerable.Empty<string>(),
            };
        }

        static ActionInfo BuildAction(ActionType type, string name, string prefix, string command, string returnType, string tableName, IEnumerable<ActionParameter> parameters = null)
        {
            return new ActionInfo
            {
                Type = type,
                Name = name,
                Command = command,
                Prefix = prefix,
                Suffix = "",
                ReturnType = returnType,
                Table = tableName,
                Parameters = parameters ?? Enumerable.Empty<ActionParameter>(),
            };
        }
    }
}
