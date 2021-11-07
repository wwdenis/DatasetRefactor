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
            var rootNamespace = first.Dataset.Namespace;

            var templates = new (string Dir, string Name, string Extension, bool IsBase, string FileName)[]
            {
                ("Main", "Row", "cs", false, null),
                ("Main", "DataTable", "cs", false, null),
                ("Main", "TableAdapter", "cs", false, null),
                ("Base", "Project", "csproj", true, rootNamespace),
                ("Base", "DbAdapter", "cs", true, null),
                ("Base", "DbRow", "cs", true, null),
                ("Base", "DbTable", "cs", true, null),
                ("Base", "DbColumnAttribute", "cs", true, null),
                ("Base", "GlobalSettings", "cs", true, null),
                ("Base", "GlobalSupressions", "cs", true, null),
            };

            foreach (var template in templates)
            {
                var selected = template.IsBase ? groups.Take(1) : groups;

                foreach (var group in selected)
                {
                    var targetDir = string.Empty;
                    var targetName = template.FileName ?? template.Name;
                    
                    if (!template.IsBase)
                    {
                        targetDir = group.Dataset.Name;
                        targetName = group.Table.Name + template.Name;
                    }

                    var targetFile = Path.ChangeExtension(targetName, template.Extension);
                    var contents = ReadTemplate(template.Name, template.Dir);
                    var file = RenderFile(group, contents, targetFile, targetDir, template.IsBase);
                    files.Add(file);
                }
            }

            return files;
        }

        private static TransformFile RenderFile(TableGroup group, string template, string targetFile, string targetDir, bool isBase)
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
                Table = group.Table.Name,
                IsBase = isBase,
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