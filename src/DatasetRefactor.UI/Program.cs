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
        private const int CodeSuccess = 0;
        private const int CodeParameterError = 1;
        private const int CodeUnknownError = 2;

        private static string currentDataset = string.Empty;

        static int Main(string[] args)
        {
            try
            {
                currentDataset = null;

                var parameters = AppParameters.Parse(args);
                if (parameters.Errors.Any())
                {
                    LogError(parameters.Errors);
                    return CodeParameterError;
                }

                LogSuccess("Reading all Datasets");
                LogSuccess($"Assembly: {parameters.AssemblyFile}");
                var files = GenerateFiles(parameters);

                LogText();
                SaveFiles(files, parameters);

                LogText();
                LogSuccess($"Finished: {files.Count()} Files written");

                return CodeSuccess;
            }
            catch (Exception ex)
            {
                LogError(ex.ToString());
                return CodeUnknownError;
            }
        }

        private static IEnumerable<TransformFile> GenerateFiles(AppParameters parameters)
        {
            var assembly = Assembly.LoadFrom(parameters.AssemblyFile);

            var scanner = new TypeScanner(assembly);
            var tableBuilder = new TableBuilder();
            var fileRenderer = new FileRenderer();
            
            tableBuilder.Progress += Builder_Progress;

            var metadata = scanner.Scan(parameters.Selected);
            var groups = tableBuilder.Build(metadata);
            var files = fileRenderer.Generate(groups);

            return files;
        }

        private static void Builder_Progress(object sender, TypeMetadata metadata)
        {
            if (currentDataset != metadata.DatasetName)
            {
                currentDataset = metadata.DatasetName;
                LogText();
                LogText(currentDataset);
            }

            LogText(metadata.AdapterName, true);
        }

        static void SaveFiles(IEnumerable<TransformFile> files, AppParameters parameters)
        {
            LogSuccess("Saving Structure:");

            var currentDir = string.Empty;

            foreach (var file in files)
            {
                SaveFile(file, parameters);

                if (currentDir != file.Directory)
                {
                    currentDir = file.Directory;
                    LogText();
                    LogText(currentDir);
                }

                LogText(file.Name, true);
            }
        }

        private static void SaveFile(TransformFile file, AppParameters parameters)
        {
            var codePath = BuildPath(file.Name, parameters.TargetDir, "Generated", file.Directory);
            var codeContents = file.Contents;
            File.WriteAllText(codePath, codeContents);

            if (parameters.SaveSource)
            {
                var jsonFile = Path.ChangeExtension(file.Adapter, "json");
                var jsonPath = BuildPath(jsonFile, parameters.TargetDir, "Sources", file.Directory);
                var jsonContents = Serialize(file.Source);

                if (!File.Exists(jsonPath))
                {
                    File.WriteAllText(jsonPath, jsonContents);
                }
            }
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

        private static void LogSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static void LogError(params string[] errors)
        {
            var error = string.Join(Environment.NewLine, errors);
            var message = string.Join(Environment.NewLine, "Errors:", error, string.Empty, AppParameters.HelpMessage);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static void LogText(string message = "", bool indent = false)
        {
            if (indent)
            {
                message = "   " + message;
            }

            Console.WriteLine(message);
        }
    }
}
