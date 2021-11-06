using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DatasetRefactor.UI
{
    class Program
    {
        const string SourceAssembly = "source";
        const string DatasetName = "dataset";

        static void Main(string[] args)
        {
            var namedArgs = ParseArgs(args);
            var assemblyFile = namedArgs[SourceAssembly];
            var datasetName = namedArgs[DatasetName];

            var assembly = Assembly.LoadFrom(assemblyFile);
            var scanner = new DatasetScanner(assembly);
            var metadata = scanner.Scan(datasetName);
            var json = Serialize(metadata);

            Console.WriteLine(json);
        }

        static string Serialize(object metadata)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Converters = new[] { new StringEnumConverter() }
            };

            return JsonConvert.SerializeObject(metadata, settings);
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
            
            if (!parameters.TryGetValue(DatasetName, out var datasetName))
            {
                parameters.Add(DatasetName, string.Empty);
            }

            return parameters;
        }
    }
}
