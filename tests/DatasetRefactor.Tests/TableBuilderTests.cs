using System.Collections.Generic;
using DatasetRefactor.Tests.Infrastructure;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DatasetRefactor.Tests
{
    public class TableBuilderTests 
    {
        private readonly ITestOutputHelper output;

        public TableBuilderTests(ITestOutputHelper output)
        {
            this.output = output;
        }

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

            using var compiler = new CodeCompilerFixture(RootNamespace, DatasetName);

            var expectedResult = compiler.BuildTableGroup(TableName, KeyColumn, columns);
            var assembly = compiler.CompileDataset(TableName, KeyColumn, columns);

            assembly.Should().NotBeNull();

            var subject = new TableBuilder(assembly);
            var result = subject.Build();

            result
                .Should()
                .BeEquivalentTo(expectedResult);
        }
    }
}
