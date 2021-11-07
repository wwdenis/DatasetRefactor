using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using DatasetRefactor.Models;
using FluentAssertions;
using HashScript;
using Microsoft.CSharp;
using Xunit;

namespace DatasetRefactor.Tests
{
    public class DatasetScannerTests
    {
        [Fact]
        public void Should_Generate()
        {
            const string RootNamespace = "EmployeeTest";
            const string DatasetName = "HumanResourcesDS";
            const string TableName = "Employee";
            const string KeyColumn = "Id";
            var columns = new Dictionary<string, string>
            {
                { "Id", "int" },
                { "Name", "string" },
            };


            var expectedResult = BuildTableGroup(RootNamespace, DatasetName, TableName, KeyColumn, columns);
            var sourceCode = BuildDatasetCode(RootNamespace, DatasetName, TableName, KeyColumn, columns);
            var success = TryBuildAssembly(RootNamespace, sourceCode, out var assembly);

            success.Should().BeTrue();

            var subject = new DatasetScanner(assembly);
            var result = subject.Scan();

            result
                .Should()
                .BeEquivalentTo(expectedResult);
        }

        static string BuildDatasetCode(string rootNamespace, string datasetName, string tableName, string keyColumn, Dictionary<string, string> columns)
        {
            var datasetInfo = BuildDatasetData(datasetName, tableName, keyColumn, columns);

            var templateContents = File.ReadAllText(@"Samples\DatasetSchema.hz");
            var templateWriter = new Writer(templateContents);
            var schema = templateWriter.Generate(datasetInfo);

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

        private static bool TryBuildAssembly(string rootNamespace, string datasetSource, out Assembly assembly)
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

            var parameters = new CompilerParameters(dependencies, $"{rootNamespace}.dll", true);
            var result = csc.CompileAssemblyFromSource(parameters, datasetSource, SettingsSource);

            assembly = result.CompiledAssembly;
            return !result.Errors.HasErrors;
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

        private TableGroup[] BuildTableGroup(string rootNamespace, string datasetName, string tableName, string keyColumn, Dictionary<string, string> columns)
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
                Suffix = $"By{keyColumn}",
                Table = tableName,
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
                                      Name = "",
                                  };

            return new[]
            {
                new TableGroup
                {
                    Dataset = new DatasetInfo
                    {
                        Name = datasetName,
                        Namespace = rootNamespace,
                    },
                    Table = new TableInfo
                    {
                        Name = tableName,
                        Namespace = string.Join(".", rootNamespace, datasetName),
                        Columns = columnInfo,
                        Actions = new[] { findAction },
                    },
                    Adapter = new AdapterInfo
                    {
                        Name = tableName + "TableAdapter",
                        Namespace = string.Join("", rootNamespace, ".", datasetName, "TableAdapters"),
                        Commands = adapterCommands,
                        Insert = BuildAction(ActionType.Insert, "Insert", "int", tableName, insertParameters),
                        Delete = BuildAction(ActionType.Delete, "Delete", "int", tableName, deleteParameters),
                        Update = BuildAction(ActionType.Update, "Update", "int", tableName, updateParameters),
                        Select = new[]
                        {
                            BuildAction(ActionType.Select, "GetData", $"{tableName}DataTable", tableName)
                        },
                    }

                }
            };
        }

        static ActionInfo BuildAction(ActionType type, string name, string returnType, string tableName, IEnumerable<ActionParameter> parameters = null)
        {
            return new ActionInfo
            {
                Type = type,
                Name = name,
                Suffix = "",
                ReturnType = returnType,
                Table = tableName,
                Parameters = parameters ?? Enumerable.Empty<ActionParameter>(),
            };
        }
    }
}
