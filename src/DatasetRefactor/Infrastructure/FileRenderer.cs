using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DatasetRefactor.Entities;
using HashScript;
using HashScript.Providers;

namespace DatasetRefactor.Infrastructure
{
    internal class FileRenderer
    {
        public IEnumerable<TransformFile> Generate(ScanResult result, TemplateGroup templates)
        {
            var files = new List<TransformFile>();

            var adapterItems = result.Items;
            var tableItems = result.Items.Where(i => i.Table != null);

            if (!result.Items.Any())
            {
                return files;
            }

            foreach (var template in templates.Adapter)
            {
                foreach (var item in adapterItems)
                {
                    var adapterFile = RenderDataFile(item, template);
                    files.Add(adapterFile);
                }
            }

            foreach (var template in templates.Table)
            {
                foreach (var item in tableItems)
                {
                    var adapterFile = RenderDataFile(item, template);
                    files.Add(adapterFile);
                }
            }

            foreach (var template in templates.Base)
            {
                var targetFile = Path.ChangeExtension(template.Name, "cs");
                var baseFile = RenderFile(result, null, template.Contents, targetFile, null);
                files.Add(baseFile);
            }

            if (templates.Project != null)
            {
                var targetFile = $"{result.Root.Namespace}.csproj";
                var projFile = RenderFile(result, null, templates.Project.Contents, targetFile, null);
                files.Add(projFile);
            }

            return files;
        }

        private static TransformFile RenderDataFile(ScanInfo info, TemplateFile template)
        {
            var adapter = info.Adapter.Name;
            var targetDir = info.Dataset.Name;
            var targetPrefix = info.Table?.Name ?? info.Adapter.Name;
            var targetName = targetPrefix + template.Name;
            var targetFile = Path.ChangeExtension(targetName, "cs");

            return RenderFile(info, adapter, template.Contents, targetFile, targetDir);
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
    }
}