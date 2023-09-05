using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Manadev.Sync
{
    class Synchronizer
    {
        public static void SyncDirectory(string extensionPath, string projectPath)
        {
            if (Directory.Exists(extensionPath))
            {
                if (Options.DeleteExistingFiles)
                {
                    if (SymbolicLink.DirectoryExists(projectPath))
                    {
                        UnsyncDirectory(projectPath);
                    }
                    else if (Directory.Exists(projectPath))
                    {
                        Directory.Delete(projectPath, true);
                    }
                }
                if (!SymbolicLink.DirectoryExists(projectPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(projectPath));
                    SymbolicLink.CreateDirectoryLink(projectPath, extensionPath);
                }
            }
            else
            {
                if (SymbolicLink.DirectoryExists(projectPath))
                {
                    UnsyncDirectory(projectPath);
                }
            }
        }
        public static void SyncFile(string extensionPath, string projectPath)
        {
            if (File.Exists(extensionPath))
            {
                if (Options.DeleteExistingFiles)
                {
                    if (SymbolicLink.FileExists(projectPath))
                    {
                        UnsyncFile(projectPath);
                    }
                    else if (File.Exists(projectPath))
                    {
                        File.Delete(projectPath);
                    }
                }

                if (!SymbolicLink.FileExists(projectPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(projectPath));
                    SymbolicLink.CreateFileLink(projectPath, extensionPath);
                }
            }
            else
            {
                if (SymbolicLink.FileExists(projectPath))
                {
                    UnsyncFile(projectPath);
                }
            }
        }

        internal static void UnsyncFile(string path)
        {
            File.Delete(path);
            DeleteDirectoryIfEmpty(Path.GetDirectoryName(path));
        }

        internal static void UnsyncDirectory(string path)
        {
            Directory.Delete(path);
            DeleteDirectoryIfEmpty(Path.GetDirectoryName(path));
        }

        private static void DeleteDirectoryIfEmpty(string path)
        {
            var directories = Directory.GetDirectories(path);
            var files = Directory.GetFiles(path);
            if (directories.Length == 0 && files.Length == 0)
            {
                Directory.Delete(path);
                DeleteDirectoryIfEmpty(Path.GetDirectoryName(path));
            }
        }
    }
}
