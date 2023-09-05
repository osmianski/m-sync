using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;

namespace Manadev.Sync
{
    public class Extension
    {
        public Project Project { get; set; }
        public string Path { get; set; }
        public string[] QualifiedName { get; set; }

        public static Extension Create(Project project, string extensionPath)
        {
            var extension = new Extension();
            extension.Project = project;
            extension.Path = project.Platform.GetPath(extensionPath, "extension.path", new string[] {project.Path});
            XDocument xdoc = XDocument.Load(project.Platform.GetPath(extension.Path, "extension.definition"));
            var name = xdoc.Element("config") != null ? xdoc.Element("config").Element("name") : null;
            if (name != null)
            {
                extension.QualifiedName = name.Value.Replace("/", "\\").Split(new Char[] { '\\' });
            }
            else 
            {
                extension.QualifiedName = project.Platform.GetQualifiedExtensionName(extensionPath);
            }
            return extension;
        }

        public string[] Synchronize()
        {
            return Synchronize(null);
        }
        public string[] Synchronize(List<FileChange> changes)
        {
            var result = new List<string>();
            if (!Project.CheckPathInMSyncXml(Path)) 
            {
                return result.ToArray();
            }
            var filename = Project.Platform.GetPath(Path, "extension.definition");
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException(string.Format("Extension definition file '{0}' not found.", 
                    filename));
            }
            XDocument xdoc = XDocument.Load(Project.Platform.GetPath(Path, "extension.definition"));
            foreach (var sync in xdoc.Descendants("sync"))
            {
                if (sync.Attribute("extension-dir") != null && sync.Attribute("project-dir") != null)
                {
                    var extensionPath = PreparePath(Path + "\\" + sync.Attribute("extension-dir").Value);
                    var projectPath = PreparePath(Project.Path + "\\" + sync.Attribute("project-dir").Value);
                    result.Add(projectPath);
                    if (changes == null || changes.Affects(extensionPath))
                    {
                        Synchronizer.SyncDirectory(extensionPath, projectPath);
                    }
                }
                else if (sync.Attribute("extension-file") != null && sync.Attribute("project-file") != null)
                {
                    var extensionPath = PreparePath(Path + "\\" + sync.Attribute("extension-file").Value);
                    var projectPath = PreparePath(Project.Path + "\\" + sync.Attribute("project-file").Value);
                    result.Add(projectPath);
                    if (changes == null || changes.Affects(extensionPath))
                    {
                        Synchronizer.SyncFile(extensionPath, projectPath);
                    }
                }
            }
            return result.ToArray();
        }

        protected string PreparePath(string path)
        {
            return path
                .Replace("{Extension_Name}", GetOriginalName("_"))
                .Replace("{Extension/Name}", GetOriginalName("/"))
                .Replace("{extension_name}", GetLowerCasedName("_"))
                .Replace("{extension/name}", GetLowerCasedName("/"))
                .Replace("/", "\\");
        }

        private string GetLowerCasedName(string separator)
        {
            var builder = new StringBuilder();
            var separatorNeeded = false;
            foreach (var name in QualifiedName)
            {
                if (separatorNeeded) builder.Append(separator); else separatorNeeded = true;
                builder.Append(name.ToLower());
            }
            return builder.ToString();
        }

        private string GetOriginalName(string separator)
        {
            return String.Join(separator, QualifiedName);
        }
    }
}
