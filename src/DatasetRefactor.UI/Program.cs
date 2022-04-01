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
                var parameters = AppParameters.Parse(args);
                if (parameters.Errors.Any())
                {
                    var error = string.Join(Environment.NewLine, parameters.Errors);
                    var message = string.Join(Environment.NewLine, "Errors:", error, string.Empty, AppParameters.HelpMessage);
                    LogError(message);
                    return 1;
                }

                LogSuccess($"Reading all Datasets from {parameters.AssemblyFile}");

                var assembly = Assembly.LoadFrom(parameters.AssemblyFile);

                var scanner = new TypeScanner(assembly);
                var codeBuilder = new CodeBuilder();
                var tableBuilder = new TableGroupBuilder();

                tableBuilder.Progress += Builder_Progress;

                var metadata = scanner.Scan(parameters.Selected);
                var groups = tableBuilder.Build(metadata);
                var files = codeBuilder.Generate(groups);

                if (parameters.SaveSource)
                {
                    SaveStructure(files, parameters.TargetDir);
                }

                SaveGenerated(files, parameters.TargetDir);

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
