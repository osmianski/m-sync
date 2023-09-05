using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Manadev.Sync.Exceptions;
using System.IO;
using System.Xml.Linq;

namespace Manadev.Sync
{
    public class Project
    {
        public string Path { get; set; }
        public Platform Platform { get; set; }
        private XDocument mSyncXml = null;

        public static Project Create(string path)
        {
            var platform = Platform.Detect(path);
            if (platform != null)
            {
                var project = new Project();
                project.Path = path;
                project.Platform = platform;
                return project;
            }
            else 
            {
                throw new UnknownPlatformException(String.Format("Unknown platform is used in '{0}'", path));
            }
        }

        public string[] Synchronize()
        {
            return Synchronize(null);
        }

        public string[] Synchronize(List<FileChange> changes)
        {
            var result = new List<string>();
            var extensions = Platform.FindAllProjectExtensions(Path);
            foreach (var extensionPath in extensions)
            {
                var extension = Extension.Create(this, extensionPath);
                
                result.AddRange(extension.Synchronize(changes));
            }
            UpdateGitIgnore(result);
            RemoveObsoleteSymbolicLinks(changes, result);
            return result.ToArray();
        }

        protected void UpdateGitIgnore(List<string> result)
        {
            if (File.Exists(Path + "\\.gitignore.m-sync.template"))
            {
                using (StreamWriter file = new StreamWriter(Path + "\\.gitignore"))
                {
                    using (StreamReader template = new StreamReader(Path + "\\.gitignore.m-sync.template"))
                    {
                        var line = "";
                        while ((line = template.ReadLine()) != null)
                        {
                            file.WriteLine(line);
                        }
                        foreach (var path in result)
                        {
                            file.WriteLine(
                                path.Substring(Path.Length).Replace("\\", "/") +
                                (Directory.Exists(path) ? "/" : ""));
                        }
                    }
                }
            }
        }

        private void RemoveObsoleteSymbolicLinks(List<FileChange> changes, List<string> relevantSymbolicLinks)
        {
            if (changes == null || changes.Affects(Path))
            {
                RemoveObsoleteSymbolicLinksInDirectory(Path, relevantSymbolicLinks);
            }
        }

        private void RemoveObsoleteSymbolicLinksInDirectory(string path, List<string> relevantSymbolicLinks)
        {
            if (System.IO.Path.GetFileName(path) == "node_modules") {
                return;
            }
            if (Directory.Exists(path))
            {
                foreach (var filename in Directory.GetDirectories(path))
                {
                    if (SymbolicLink.DirectoryExists(filename) && !relevantSymbolicLinks.Contains(filename))
                    {
                        Synchronizer.UnsyncDirectory(filename);
                    }
                    else
                    {
                        RemoveObsoleteSymbolicLinksInDirectory(filename, relevantSymbolicLinks);
                    }
                }
            }

            if (Directory.Exists(path))
            {
                foreach (var filename in Directory.GetFiles(path))
                {
                    if (SymbolicLink.FileExists(filename) && !relevantSymbolicLinks.Contains(filename))
                    {
                        Synchronizer.UnsyncFile(filename);
                    }
                }
            }
        }

        internal bool CheckPathInMSyncXml(string path)
        {
            if (mSyncXml == null)
            {
                if (File.Exists(Path + "\\.team-config"))
                {
                    mSyncXml = XDocument.Load(Path + "\\.team-config");
                }
                else
                {
                    return false;
                }
            }

            if (mSyncXml.Element("config") != null && mSyncXml.Element("config").Element("m-sync") != null)
            {
                foreach (var rule in mSyncXml.Element("config").Element("m-sync").Descendants("exclude"))
                {
                    if (rule.Attribute("dir") != null) 
                    {
                        var rulePath = rule.Attribute("dir").Value.Replace("/", "\\");
                        if (path.StartsWith(Platform.GetPath(rulePath, "extension.path", new string[] { Path })))
                        {
                            return false;
                        }
                    }
                }
                foreach (var rule in mSyncXml.Element("config").Element("m-sync").Descendants("include"))
                {
                    if (rule.Attribute("dir") != null)
                    {
                        var rulePath = rule.Attribute("dir").Value.Replace("/", "\\");
                        if (path.StartsWith(Platform.GetPath(rulePath, "extension.path", new string[] { Path })))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
