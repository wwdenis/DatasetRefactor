using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data.Design;
using System.IO;
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
            var expectedResult = BuildTableGroup("EmployeeTest");
            var sourceCode = GenerateDataset("EmployeeTest");
            var buildResult = CompileCode(sourceCode);

            buildResult
                .Errors
                .Count
                .Should()
                .Be(0);

            var subject = new DatasetScanner(buildResult.CompiledAssembly);
            var result = subject.Scan();

            result
                .Should()
                .BeEquivalentTo(
                    expectedResult,
                    opts => opts.Including(i => i.Dataset));
        }

        static string GenerateDataset(string projectName)
        {
            var templateContents = File.ReadAllText(@"Samples\DatasetSchema.hz");
            var templateWriter = new Writer(templateContents);
            var templateData = BuildDatasetData();
            var schema = templateWriter.Generate(templateData);

            var rootNamespace = new CodeNamespace(projectName);
            var unit = new CodeCompileUnit();

            using var provider = new CSharpCodeProvider();
            using var writer = new StringWriter();

            TypedDataSetGenerator.Generate(schema, unit, rootNamespace, provider, TypedDataSetGenerator.GenerateOption.HierarchicalUpdate);
            provider.GenerateCodeFromNamespace(rootNamespace, writer, null);
            provider.GenerateCodeFromCompileUnit(unit, writer, null);

            var output = writer.ToString();
            return output;
        }

        private static CompilerResults CompileCode(string datasetSource)
        {
            const string SettingsSource =
                @"namespace MyApp.Properties.Settings {
                    internal class Default {
                        public static string MyDatabaseConnectionString = ""Test"";
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

            var parameters = new CompilerParameters(dependencies, "Test.dll", true);
            var results = csc.CompileAssemblyFromSource(parameters, datasetSource, SettingsSource);
            return results;
        }

        private TableGroup[] BuildTableGroup(string rootNamespace)
        {
            return new[]
            {
                new TableGroup
                {
                    Dataset = new DatasetInfo
                    {
                        Name = "HumanResourcesDS",
                        Namespace = rootNamespace,
                    }
                }
            };
        }

        static Dictionary<string, object> BuildDatasetData()
        {
            return new Dictionary<string, object>()
            {
                { "Dataset", "HumanResourcesDS" },
                { "Table", "Employee" },
                { "PrimaryKey", "Id" },
                { "Select", "SELECT * FROM Employee" },
                { "Insert", "INSERT INTO Employee" },
                { "Delete", "DELETE FROM Employee" },
                { "Update", "UPDATE Employee" },
                { 
                    "Columns",
                    new[]
                    {
                        new Dictionary<string, object>()
                        {
                            { "Name", "Id" },
                            { "Type", "Int32" },
                            { "DbType", "Int" },
                            { "XsType", "int" },
                            { "IsRequired", false },
                            { "IsKey", true },
                        },
                        new Dictionary<string, object>()
                        {
                            { "Name", "Name" },
                            { "Type", "String" },
                            { "DbType", "NVarChar" },
                            { "XsType", "string" },
                            { "IsRequired", true },
                            { "IsKey", false },
                        }
                    }
                }, 
            };
        }
    }
}
