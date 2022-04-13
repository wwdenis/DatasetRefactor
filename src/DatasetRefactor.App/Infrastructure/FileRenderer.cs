using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DatasetRefactor.Entities;
using HashScript;
using HashScript.Providers;

namespace DatasetRefactor.App.Infrastructure
{
    public class FileRenderer
    {
        public IEnumerable<TransformFile> Generate(ScanResult result)
        {
            var files = new List<TransformFile>();

            if (!result.Items.Any())
            {
                return files;
            }

            var projectFile = $"{result.Root.Namespace}.csproj";

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

            foreach (var group in result.Items)
            {
                foreach (var template in mainTemplates)
                {
                    var templateName = template.Key;
                    var isAdapter = template.Value;
                    if (!isAdapter && group.Table is null)
                    {
                        continue;
                    }

                    var adapter = group.Adapter.Name;
                    var targetDir = group.Dataset.Name;
                    var targetPrefix = group.Table?.Name ?? group.Adapter.Name;
                    var targetName = targetPrefix + templateName;
                    var targetFile = Path.ChangeExtension(targetName, "cs");
                    var contents = ReadTemplate(templateName, "Main");
                    var file = RenderFile(group, adapter, contents, targetFile, targetDir);
                    files.Add(file);
                }
            }

            foreach (var template in baseTemplates)
            {
                var templateName = template.Key;
                var targetFile = template.Value ?? Path.ChangeExtension(templateName, "cs");
                var contents = ReadTemplate(templateName, "Base");
                var file = RenderFile(result, null, contents, targetFile, null);
                files.Add(file);
            }
            
            return files;
        }

        private static TransformFile RenderFile(object source, string name, string template, string targetFile, string targetDir)
        {
            var provider = new ObjectValueProvider(source);
            var renderer = new Renderer(template);
            var generated = renderer.Generate(provider);

            return new TransformFile
            {
                Name = targetFile,
                Directory = targetDir,
                Contents = generated,
                SourceData = source,
                SourceName = name,
            };
        }

        private static string ReadTemplate(string templateName, string templateDir)
        {
            var assembly = typeof(FileRenderer).Assembly;
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