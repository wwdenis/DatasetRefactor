using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DatasetRefactor.Entities;
using HashScript;
using HashScript.Providers;

namespace DatasetRefactor.Infrastructure
{
    public class FileRenderer
    {
        public IEnumerable<TransformFile> Generate(ScanResult result)
        {
            var files = new List<TransformFile>();

            var adapterItems = result.Items;
            var tableItems = result.Items.Where(i => i.Table != null);

            if (!result.Items.Any())
            {
                return files;
            }

            var assembly = typeof(FileRenderer).Assembly;
            var templateFiles = assembly.GetManifestResourceNames();
            var templateContents = templateFiles.ToDictionary(k => k, v => ReadTemplate(v));

            var tableTemplates = templateContents.Where(i => i.Key.Contains(".Table."));
            var adapterTemplates = templateContents.Where(i => i.Key.Contains(".Adapter."));
            var baseTemplates = templateContents.Where(i => i.Key.Contains(".Base."));
            var projectTemplates = templateContents.Where(i => i.Key.Contains(".Project."));

            foreach (var template in adapterTemplates)
            {
                foreach (var item in adapterItems)
                {
                    var adapterFile = RenderDataFile(item, template.Key, template.Value);
                    files.Add(adapterFile);
                }
            }

            foreach (var template in tableTemplates)
            {
                foreach (var item in tableItems)
                {
                    var adapterFile = RenderDataFile(item, template.Key, template.Value);
                    files.Add(adapterFile);
                }
            }

            foreach (var template in baseTemplates)
            {
                var templateName = ParseTemplateName(template.Key);
                var targetFile = Path.ChangeExtension(templateName, "cs");
                var baseFile = RenderFile(result, null, template.Value, targetFile, null);
                files.Add(baseFile);
            }

            foreach (var template in projectTemplates)
            {
                var targetFile = $"{result.Root.Namespace}.csproj";
                var projFile = RenderFile(result, null, template.Value, targetFile, null);
                files.Add(projFile);
            }

            return files;
        }

        private static TransformFile RenderDataFile(ScanInfo info, string templatePath, string templateContents)
        {
            var templateName = ParseTemplateName(templatePath);

            var adapter = info.Adapter.Name;
            var targetDir = info.Dataset.Name;
            var targetPrefix = info.Table?.Name ?? info.Adapter.Name;
            var targetName = targetPrefix + templateName;
            var targetFile = Path.ChangeExtension(targetName, "cs");

            return RenderFile(info, adapter, templateContents, targetFile, targetDir);
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

        public static string ParseTemplateName(string templatePath)
        {
            var fragments = templatePath.Split('.');

            if (fragments.Count() < 2)
            {
                return string.Empty;
            }

            return fragments.ElementAtOrDefault(fragments.Count() - 2);
        }

        private static string ReadTemplate(string templatePath)
        {
            var assembly = typeof(FileRenderer).Assembly;
            using var stream = assembly.GetManifestResourceStream(templatePath);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}