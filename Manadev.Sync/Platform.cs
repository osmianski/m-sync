using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Manadev.Sync.Platforms;

namespace Manadev.Sync
{
    public abstract class Platform
    {
        static protected Platform[] knownPlatforms = { new MagentoPlatform() };
        static public Platform[] KnownPlatforms { get { return knownPlatforms; } }

        static public Platform Detect(string path)
        {
            foreach (var platform in KnownPlatforms)
            {
                if (platform.IsUsedIn(path))
                {
                    return platform;
                }
            }
            return null;
        }

        abstract protected bool IsUsedIn(string path);

        public string GetPath(string path, string type)
        {
            return GetPath(path, type, new string[] {});
        }

        virtual public string GetPath(string path, string type, string[] args)
        {
            switch (type)
            {
                case "extension.path":
                    return string.Format("{0}\\vendor\\{1}", args[0], path);
                case "extension.definition":
                    return string.Format("{0}\\{1}", path, "extension.xml");
                default:
                    throw new NotImplementedException();
            }
        }

        internal string[] GetQualifiedExtensionName(string path)
        {
            var allParts = path.Replace("/", "\\").Split(new Char[] { '\\' }); 
            var result = new string[allParts.Length - 1];
            Array.Copy(allParts, 1, result, 0, result.Length);
            return result;
        }

        abstract public string[] FindAllProjectExtensions(string path);
    }
}
