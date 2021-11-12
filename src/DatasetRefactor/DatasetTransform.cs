using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DatasetRefactor.Extensions;
using DatasetRefactor.Models;
using HashScript;

namespace DatasetRefactor
{
    public class DatasetTransform
    {
        private readonly Assembly assembly;

        public DatasetTransform(Assembly assembly)
        {
            this.assembly = assembly;
        }

        public IEnumerable<TransformFile> Generate(string tableFilter = null)
        {
            var scanner = new DatasetScanner(assembly);
            var groups = scanner.Scan(tableFilter);
            var files = new List<TransformFile>();

            if (!groups.Any())
            {
                return files;
            }

            var first = groups.First();
            var projectFile = Path.ChangeExtension(first.Dataset.Namespace, "csproj");

            var mainTemplates = new Dictionary<string, bool>()
            {
                { "TableAdapter", true },
                { "DataTable", false },
                { "Row", false },
            };

            var baseTemplates = new Dictionary<string, string>()
            {
                { "DbAdapter", null },
                { "DbTable", null },
                { "DbRow", null },
                { "DbColumnAttribute", null },
                { "GlobalSettings", null },
                { "GlobalSupressions", null },
                { "Project", projectFile },
            };

            foreach (var group in groups)
            {
                foreach (var template in mainTemplates)
                {
                    var templateName = template.Key;
                    var isAdapter = template.Value;
                    if (!isAdapter && group.Table is null)
                    {
                        continue;
                    }

                    var targetDir = group.Dataset.Name;
                    var targetPrefix = group.Table?.Name ?? group.Adapter.Name;
                    var targetName = targetPrefix + templateName;
                    var targetFile = Path.ChangeExtension(targetName, "cs");
                    var contents = ReadTemplate(templateName, "Main");
                    var file = RenderFile(group, contents, targetFile, targetDir);
                    files.Add(file);
                }
            }

            foreach (var template in baseTemplates)
            {
                var templateName = template.Key;
                var targetFile = template.Value ?? Path.ChangeExtension(templateName, "cs");
                var contents = ReadTemplate(templateName, "Base");
                var file = RenderFile(first, contents, targetFile, null);
                files.Add(file);
            }
            
            return files;
        }

        private static TransformFile RenderFile(TableGroup group, string template, string targetFile, string targetDir)
        {
            var source = group.ToDictionary();
            var writer = new Writer(template);
            var generated = writer.Generate(source);

            return new TransformFile
            {
                Name = targetFile,
                Directory = targetDir,
                Contents = generated,
                Source = source,
                Adapter = group.Adapter.Name,
                IsBase = string.IsNullOrEmpty(targetDir),
            };
        }

        private static string ReadTemplate(string templateName, string templateDir)
        {
            var assembly = typeof(DatasetTransform).Assembly;
            var assemblyName = assembly.GetName().Name;
            var fragments = new[] { assemblyName, "Templates", templateDir, templateName, "hz" };
            var templatePath = string.Join(".", fragments.Where(i => i != null));

            using var stream = assembly.GetManifestResourceStream(templatePath);
            using var reader = new StreamReader(stream);
            var template = reader.ReadToEnd();

            return template;
        }
    }
}