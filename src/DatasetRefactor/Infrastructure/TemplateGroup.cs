using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DatasetRefactor.Infrastructure
{
    internal class TemplateGroup
    {
        public TemplateFile Project { get; set; }

        public IEnumerable<TemplateFile> Base { get; set; }

        public IEnumerable<TemplateFile> Adapter { get; set; }

        public IEnumerable<TemplateFile> Table { get; set; }

        public bool IsValid()
        {
            if (this.Project is null && this.Base is null && this.Adapter is null && this.Table is null)
            {
                return false;
            }

            return this.Project.IsValid()
                || this.Base.Any(i => i.IsValid())
                || this.Adapter.Any(i => i.IsValid())
                || this.Table.Any(i => i.IsValid());
        }

        public static TemplateGroup ReadAll(string templateDir)
        {
            var externalTemplates = ReadExternalTemplates(templateDir);

            if (externalTemplates.IsValid())
            {
                return externalTemplates;
            }

            return ReadEmbeddedTemplates();
        }

        private static TemplateGroup ReadExternalTemplates(string templateDir)
        {
            if (!Directory.Exists(templateDir))
            {
                return new TemplateGroup();
            }

            var files = Directory.GetFiles(templateDir, "*.hz", SearchOption.AllDirectories);
            var templates = files.ToDictionary(k => k, v => File.ReadAllText(v));

            return new TemplateGroup
            {
                Project = ParseExternalTemplate(templates, "Project").FirstOrDefault(),
                Base = ParseExternalTemplate(templates, "Base"),
                Adapter = ParseExternalTemplate(templates, "Adapter"),
                Table = ParseExternalTemplate(templates, "Table"),
            };
        }

        private static TemplateGroup ReadEmbeddedTemplates()
        {
            var assembly = typeof(FileRenderer).Assembly;
            var files = assembly.GetManifestResourceNames();
            var templates = files.ToDictionary(k => k, v => ReadEmbedded(v));

            return new TemplateGroup
            {
                Project = ParseEmbeddedTemplate(templates, "Project").FirstOrDefault(),
                Base = ParseEmbeddedTemplate(templates, "Base"),
                Adapter = ParseEmbeddedTemplate(templates, "Adapter"),
                Table = ParseEmbeddedTemplate(templates, "Table"),
            };
        }

        private static IEnumerable<TemplateFile> ParseExternalTemplate(IDictionary<string, string> templates, string category)
        {
            return from i in templates
                   let dir = Directory.GetParent(i.Key)
                   where dir != null && dir.Name == category
                   select new TemplateFile
                   {
                       Name = Path.GetFileNameWithoutExtension(i.Key),
                       Path = i.Key,
                       Contents = i.Value,
                   };
        }

        private static IEnumerable<TemplateFile> ParseEmbeddedTemplate(IDictionary<string, string> templates, string category)
        {
            return from i in templates
                   let dir = ParseEmbeddedName(i.Key, 3)
                   where dir == category
                   select new TemplateFile
                   {
                       Name = ParseEmbeddedName(i.Key, 2),
                       Path = i.Key,
                       Contents = i.Value,
                   };
        }

        private static string ParseEmbeddedName(string templatePath, int position = 1)
        {
            var fragments = templatePath.Split('.');

            if (fragments.Count() < position)
            {
                return string.Empty;
            }

            return fragments.ElementAtOrDefault(fragments.Count() - position);
        }

        private static string ReadEmbedded(string templatePath)
        {
            var assembly = typeof(FileRenderer).Assembly;
            using var stream = assembly.GetManifestResourceStream(templatePath);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
