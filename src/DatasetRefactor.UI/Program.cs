using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DatasetRefactor.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DatasetRefactor.UI
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                if (!ParseArgs(args, out var assemblyFile, out var targetDir, out var saveSource, out var tableName))
                {
                    return 1;
                }

                LogSuccess($"Reading all Datasets from {assemblyFile}");

                var assembly = Assembly.LoadFrom(assemblyFile);

                var scanner = new TypeScanner(assembly);
                var codeBuilder = new CodeBuilder();
                var tableBuilder = new TableGroupBuilder();

                tableBuilder.Progress += Builder_Progress;

                var metadata = scanner.Scan();
                var groups = tableBuilder.Build(metadata);
                var files = codeBuilder.Generate(groups);

                if (saveSource == "1")
                {
                    SaveStructure(files, targetDir);
                }

                SaveGenerated(files, targetDir);

                LogSuccess($"Finished: {files.Count()} Files written");

                return 0;
            }
            catch (Exception ex)
            {
                LogError(ex.ToString());
                return 2;
            }
        }

        private static void Builder_Progress(object sender, string message)
        {
            Log(message);
        }

        static string Serialize(object data)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Converters = new[] { new StringEnumConverter() }
            };

            return JsonConvert.SerializeObject(data, settings);
        }

        private static bool ParseArgs(string[] args, out string assemblyFile, out string targetDir, out string saveSource, out string tableName)
        {
            var errorMessage = string.Empty;
            assemblyFile = string.Empty;
            targetDir = string.Empty;
            saveSource = string.Empty;
            tableName = string.Empty;

            var parameters = args
                .Select(i => i.Split('='))
                .ToDictionary(k => k.ElementAtOrDefault(0), v => v.ElementAtOrDefault(1));

            parameters.TryGetValue("source", out assemblyFile);
            parameters.TryGetValue("target", out targetDir);
            parameters.TryGetValue("save", out saveSource);
            parameters.TryGetValue("table", out tableName);

            if (!File.Exists(assemblyFile))
            {
                errorMessage = $"SourceAssembly {assemblyFile} does not exist";
            }

            if (!Directory.Exists(targetDir))
            {
                errorMessage = $"TargetDir {targetDir} does not exist";
            }

            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                return true;
            }

            errorMessage = $"Error: {errorMessage}\n\nUsage: untier -source=[assembly] -target=[directory] -save=[0/1] -dataset=[name]";
            LogError(errorMessage);
            return false;
        }

        static void SaveStructure(IEnumerable<TransformFile> allFiles, string targetRoot)
        {
            Console.CursorTop++;
            Console.WriteLine("Saving Structure:");
            Console.CursorTop++;

            var files = allFiles
                .Where(i => !i.IsBase)
                .GroupBy(p => p.Adapter)
                .Select(g => g.First());

            foreach (var file in files)
            {
                var fileName = Path.ChangeExtension(file.Adapter, "json");
                var path = BuildPath(fileName, targetRoot, "Sources", file.Directory);
                var json = Serialize(file.Source);

                File.WriteAllText(path, json);
                Console.WriteLine(path);
            }

            Console.CursorTop++;
        }

        private static void SaveGenerated(IEnumerable<TransformFile> files, string targetRoot)
        {
            Console.CursorTop++;
            Console.WriteLine("Saving Generated:");
            Console.CursorTop++;

            foreach (var file in files)
            {
                var path = BuildPath(file.Name, targetRoot, "Generated", file.Directory);

                File.WriteAllText(path, file.Contents);
                Console.WriteLine(path);
            }

            Console.CursorTop++;
        }

        private static string BuildPath(string fileName, params string[] dirs)
        {
            var path = string.Empty;
            var validDirs = dirs.Where(i => !string.IsNullOrWhiteSpace(i));

            foreach (var dir in validDirs)
            {
                path = Path.Combine(path, dir);

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }

            return Path.Combine(path, fileName);
        }

        private static void LogSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static void LogError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
