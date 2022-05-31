using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DatasetRefactor.Infrastructure;
using DatasetRefactor.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DatasetRefactor
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
                
                GenerateFiles(parameters, out var files, out var errors);

                LogText();
                SaveFiles(files, parameters);

                if (errors.Any())
                {
                    LogText();
                    LogError("Errors:");
                    foreach (var error in errors)
                    {
                        LogText(error, true);
                    }
                }

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

        private static void GenerateFiles(AppParameters parameters, out IEnumerable<TransformFile> files, out IEnumerable<string> errors)
        {
            var assembly = Assembly.LoadFrom(parameters.AssemblyFile);

            var tabelFilter = parameters.Selected.Select(i => new ScanFilter(i.Key, i.Value));
            var renderer = new FileRenderer();
            var scanner = new TableScanner(assembly);
            scanner.Progress += Scanner_Progress;

            var result = scanner.Scan(tabelFilter);
            files = renderer.Generate(result);
            errors = result.Errors;
        }

        private static void Scanner_Progress(object sender, TypeMetadata metadata)
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

            if (parameters.SaveSource && !string.IsNullOrEmpty(file.SourceName))
            {
                var jsonFile = Path.ChangeExtension(file.SourceName, "json");
                var jsonPath = BuildPath(jsonFile, parameters.TargetDir, "Sources", file.Directory);
                var jsonContents = Serialize(file.SourceData);

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

        private static void LogError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static void LogError(string[] errors)
        {
            var error = string.Join(Environment.NewLine, errors);
            var message = string.Join(Environment.NewLine, "Errors:", error, string.Empty, AppParameters.HelpMessage);
            LogError(message);
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
