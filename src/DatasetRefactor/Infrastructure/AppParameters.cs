using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DatasetRefactor.Infrastructure
{
    internal class AppParameters
    {
        public const string HelpMessage = "Usage: DatasetRefactor source=[assembly] target=[directory] templates=[directory] save=[0/1] filter=[filterFile]";

        public string AssemblyFile { get; set; }

        public string TargetDir { get; set; }

        public string TemplateDir { get; set; }

        public bool SaveSource { get; set; }

        public Dictionary<string, string[]> Selected { get; set; }

        public TemplateGroup Templates { get; set; }

        public string[] Errors { get; set; }

        public static AppParameters Parse(string[] args)
        {
            var errors = new List<string>();
            var assemblyFile = string.Empty;
            var targetDir = string.Empty;
            var saveSource = string.Empty;
            var filterFile = string.Empty;
            var templateDir = string.Empty;

            var parameters = args
                .Select(i => i.Split('='))
                .ToDictionary(k => k.ElementAtOrDefault(0), v => v.ElementAtOrDefault(1));

            parameters.TryGetValue("source", out assemblyFile);
            parameters.TryGetValue("target", out targetDir);
            parameters.TryGetValue("save", out saveSource);
            parameters.TryGetValue("filter", out filterFile);
            parameters.TryGetValue("templates", out templateDir);

            if (string.IsNullOrWhiteSpace(assemblyFile))
            {
                errors.Add($"Parameter Source is mandatory");
            }
            else if (!File.Exists(assemblyFile))
            {
                errors.Add($"Source Assembly [{assemblyFile}] does not exist");
            }

            if (string.IsNullOrWhiteSpace(targetDir))
            {
                errors.Add($"Parameter Target is mandatory");
            }
            else if (!Directory.Exists(targetDir))
            {
                errors.Add($"Target Directory [{targetDir}] does not exist");
            }

            var hasFilter = TryReadFilter(filterFile, out var error, out var selected);
            if (!hasFilter)
            {
                errors.Add(error);
            }

            if (string.IsNullOrWhiteSpace(templateDir))
            {
                templateDir = "Templates";
            }

            var templates = TemplateGroup.ReadAll(templateDir);

            return new AppParameters
            {
                AssemblyFile = assemblyFile,
                TargetDir = targetDir,
                SaveSource = saveSource == "1",
                Errors = errors.ToArray(),
                Selected = selected,
                TemplateDir = templateDir,
                Templates = templates,
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
                          select g;

            result = grouped.ToDictionary(k => k.Key, v => v.Select(i => i.Method).ToArray());

            return true;
        }
    }
}
