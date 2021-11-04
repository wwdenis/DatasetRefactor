using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DatasetRefactor.UI
{
    class Program
    {
        const string SourceAssembly = "source";
        const string TableName = "table";

        static void Main(string[] args)
        {
            var namedArgs = ParseArgs(args);
            var assemblyFile = namedArgs[SourceAssembly];
            var tableName = namedArgs[TableName];

            var builder = new DefinitionBuilder(assemblyFile);
            var metadata = builder.Build(tableName);
            var json = Serialize(metadata);

            Console.WriteLine(json);
            Console.WriteLine("DONE");
        }

        static string Serialize(object metadata)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };

            options.Converters.Add(new JsonStringEnumConverter());
            return JsonSerializer.Serialize(metadata, options);
        }

        private static Dictionary<string, string> ParseArgs(string[] args)
        {
            var parameters = args
                .Select(i => i.TrimStart('-').Split('='))
                .ToDictionary(k => k[0], v => v[1]);

            parameters.TryGetValue(SourceAssembly, out var assemblyFile);
            
            if (!File.Exists(assemblyFile))
            {
                throw new Exception($"SourceAssembly {assemblyFile} does not exist");
            }
            
            if (!parameters.TryGetValue(TableName, out var tableName))
            {
                parameters.Add(TableName, string.Empty);
            }

            return parameters;
        }
    }
}
