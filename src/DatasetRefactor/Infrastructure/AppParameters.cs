using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DatasetRefactor.Infrastructure
{
    internal class AppParameters
    {
        public const string HelpMessage = "Usage: DatasetRefactor assemblyFile=[assembly] outputRoot=[directory] templateRoot=[directory] saveData=[0/1] filterFile=[file] rootNamespace=[namespace]";

        public string AssemblyFile { get; set; }

        public string OutputRoot { get; set; }

        public string TemplateRoot { get; set; }

        public bool SaveData { get; set; }

        public string RootNamespace { get; set; }

        public Dictionary<string, string[]> Selected { get; set; }

        public TemplateGroup Templates { get; set; }

        public string[] Errors { get; set; }

        public static AppParameters Parse(string[] args)
        {
            var errors = new List<string>();
            var assemblyFile = string.Empty;
            var outputRoot = string.Empty;
            var saveData = string.Empty;
            var filterFile = string.Empty;
            var templateRoot = string.Empty;
            var rootNamespace = string.Empty;

            var parameters = args
                .Select(i => i.Split('='))
                .ToDictionary(k => k.ElementAtOrDefault(0), v => v.ElementAtOrDefault(1));

            parameters.TryGetValue("assemblyFile", out assemblyFile);
            parameters.TryGetValue("outputRoot", out outputRoot);
            parameters.TryGetValue("saveData", out saveData);
            parameters.TryGetValue("filterFile", out filterFile);
            parameters.TryGetValue("templateRoot", out templateRoot);
            parameters.TryGetValue("rootNamespace", out rootNamespace);

            if (!string.IsNullOrEmpty(rootNamespace))
            {
                rootNamespace = Regex.Replace(rootNamespace, @"[^a-zA-Z0-9\. -]", string.Empty);
            }

            if (string.IsNullOrWhiteSpace(assemblyFile))
            {
                errors.Add($"Parameter [AssemblyFile] is mandatory");
            }
            else if (!File.Exists(assemblyFile))
            {
                errors.Add($"Assembly File [{assemblyFile}] does not exist");
            }

            if (string.IsNullOrWhiteSpace(outputRoot))
            {
                errors.Add($"Parameter [OutputRoot] is mandatory");
            }
            else if (!Directory.Exists(outputRoot))
            {
                errors.Add($"Output Directory [{outputRoot}] does not exist");
            }

            var hasFilter = TryReadFilter(filterFile, out var error, out var selected);
            if (!hasFilter)
            {
                errors.Add(error);
            }

            if (string.IsNullOrWhiteSpace(templateRoot))
            {
                templateRoot = "Templates";
            }

            var templates = TemplateGroup.ReadAll(templateRoot);

            return new AppParameters
            {
                AssemblyFile = assemblyFile,
                OutputRoot = outputRoot,
                SaveData = saveData == "1",
                Errors = errors.ToArray(),
                Selected = selected,
                TemplateRoot = templateRoot,
                Templates = templates,
                RootNamespace = rootNamespace,
            };
        }

        private static bool TryReadFilter(string filterFile, out string error, out Dictionary<string, string[]> result)
        {
            result = new Dictionary<string, string[]>();
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(filterFile))
            {
                return true;
            }

            var info = new FileInfo(filterFile);
            if (!info.Exists)
            {
                error = $"Filter File [{filterFile}] does not exist";
                return false;
            }
            
            if (info.Length > Math.Pow(2, 20))
            {
                error = "Filter File cannot be larger than 1MB";
                return false;
            }

            var lines = File.ReadAllLines(filterFile);
            var parsed = new List<(string Adapter, string Method)>();

            foreach (var line in lines)
            {
                var cells = line.Split(',');
                var adapterName = cells.ElementAtOrDefault(0);
                var actionName = cells.ElementAtOrDefault(1);
                parsed.Add((adapterName, actionName));
            }

            var grouped = from i in parsed
                          where !string.IsNullOrWhiteSpace(i.Adapter)
                          group i by i.Adapter into g
                          let methods = from m in g
                                        where !string.IsNullOrWhiteSpace(m.Method)
                                        select m.Method
                          select new
                          {
                              Adapter = g.Key,
                              Methods = methods
                          };

            result = grouped.ToDictionary(k => k.Adapter, v => v.Methods.ToArray());

            return true;
        }
    }
}
