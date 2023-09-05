using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Manadev.Sync.Platforms
{
    class MagentoPlatform: Platform
    {
        override protected bool IsUsedIn(string path)
        {
            return File.Exists(path + "\\app\\Mage.php");
        }
        override public string GetPath(string path, string type, string[] args)
        {
            switch (type)
            {
                case "extension.path":
                    return string.Format("{0}\\vendor\\{1}", args[0], path);
                default:
                    return base.GetPath(path, type, args);
            }
        }
        override public string[] FindAllProjectExtensions(string path)
        { 
            var result = new List<string>();
            var rootPath = path + "\\vendor\\";
            if (Directory.Exists(rootPath))
            {
                DoFindAllProjectExtensions(rootPath, rootPath, result);
            }

            return result.ToArray();
        }

        private void DoFindAllProjectExtensions(string path, string rootPath, List<string> result)
        {
            foreach (var filename in Directory.GetDirectories(path)) 
            {
                if (File.Exists(filename + "\\extension.xml")) 
                {
                    result.Add(filename.Substring(rootPath.Length));
                }
                DoFindAllProjectExtensions(filename, rootPath, result);
            }
        }

    }
}
