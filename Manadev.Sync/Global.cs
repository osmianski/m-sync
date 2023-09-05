using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Threading;

namespace Manadev.Sync
{
    public class Global
    {
        public string[] Paths { get; set; }
        private static Global instance = null;
        public static Global Create()
        {
            if (instance == null)
            {
                instance = new Global();
            }

            return instance;
        }

        public int GetInt32(string key, int defaultValue)
        {
            var value = defaultValue;
            var strValue = Get(key);
            if (strValue.Trim() != "")
            {
                int.TryParse(strValue, out value);
            }
            return value;
        }
        public string DoGet(string key, string filename)
        {
            lock (this)
            {
                if (File.Exists(filename))
                {
                    var doc = XDocument.Load(filename);
                    var element = doc.Root.Element(key);
                    if (element != null)
                    {
                        return element.Value;
                    }
                }
                return "";
            }
        }

        public void DoSet(string key, string value, string filename)
        {
            lock (this)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filename));
                XDocument doc = null;
                if (File.Exists(filename))
                {
                    doc = XDocument.Load(filename);
                }
                else
                {
                    doc = XDocument.Parse("<?xml version=\"1.0\"?>\n<config>\n</config>\n");
                }

                var element = doc.Root.Element(key);
                if (element == null)
                {
                    element = new XElement(key);
                    doc.Root.Add(element);
                }

                element.Value = value;

                doc.Save(filename);
            }
        }

        public void Set(string key, string value)
        {
            var filename = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
                + "\\m-sync\\config.xml";
            DoSet(key, value, filename);
        }

        public string Get(string key)
        {
            var filename = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
                + "\\m-sync\\config.xml";
            return DoGet(key, filename);
        }

        public void ServiceSet(string key, string value)
        {
            var filename = Environment.GetFolderPath(Environment.SpecialFolder.System)
                + "\\config\\systemprofile\\AppData\\Roaming\\m-sync\\config.xml";
            DoSet(key, value, filename);
        }

        public string ServiceGet(string key)
        {
            var filename = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                + "\\config\\systemprofile\\AppData\\Roaming\\m-sync\\config.xml";
            return DoGet(key, filename);
        }

        public void Synchronize()
        {
            Synchronize(null);
        }

        public void Synchronize(List<FileChange> changes)
        {
            var webRoots = Get("webRoots");
            int depth = GetInt32("projectDepth", 2);

            foreach (var webRoot in webRoots.Split(new char[] {';'}))
            {
                if (webRoot.Trim() != "" && Directory.Exists(webRoot))
                {
                    SynchronizeProjects(webRoot, changes, depth);
                }
            }
        }

        private void SynchronizeProjects(string path, List<FileChange> changes, int depth)
        {
            foreach (var subDirectory in Directory.GetDirectories(path))
            {
                if (Platform.Detect(subDirectory) != null)
                {
                    var project = Project.Create(subDirectory);
                    project.Synchronize(changes);
                }
                else if (depth > 1)
                {
                    SynchronizeProjects(subDirectory, changes, depth - 1);
                }
            }
        }

        public void Watch()
        {
            var webRoots = Get("webRoots");
            foreach (var webRoot in webRoots.Split(new char[] { ';' }))
            {
                if (webRoot.Trim() != "" && Directory.Exists(webRoot))
                {
                    WatchDirectory(webRoot);
                }
            }

            ThreadPool.QueueUserWorkItem(OnProcess); 
        }

        List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
        List<FileChange> changes = new List<FileChange>();
        ManualResetEvent pendingChangesEvent = new ManualResetEvent(false);
        ManualResetEvent stopEvent = new ManualResetEvent(false);


        private void WatchDirectory(string path)
        {
            pendingChangesEvent.Reset();
            stopEvent.Reset();

            var watcher = new FileSystemWatcher(path);
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.Changed += new FileSystemEventHandler(OnChanged);

                
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);

            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
            watchers.Add(watcher);
        }

        void OnRenamed(object sender, RenamedEventArgs e)
        {
            lock (this)
            {
                changes.Add(new FileChange() { Path = e.OldFullPath, Type = WatcherChangeTypes.Deleted });
                changes.Add(new FileChange() { Path = e.FullPath, Type = WatcherChangeTypes.Created });
            }
            pendingChangesEvent.Set();
        }

        void OnChanged(object sender, FileSystemEventArgs e)
        {
            lock (this)
            {
                changes.Add(new FileChange() { Path = e.FullPath, Type = e.ChangeType });
            }
            pendingChangesEvent.Set();
        }

        private void OnProcess(Object threadContext)
        {
            bool pending = false;
            while (true)
            {
                if (!pending) 
                {
                    if (WaitHandle.WaitAny(new ManualResetEvent[] { stopEvent, pendingChangesEvent }) == 0)
                    {
                        return;
                    }
                    pending = true;
                }
                else 
                {
                    int delay = GetInt32("watchDelay", 200);
                    
                    pendingChangesEvent.Reset();
                    if (!pendingChangesEvent.WaitOne(delay))
                    {
                        pending = false;
                        List<FileChange> currentChanges = null;
                        lock (this)
                        {
                            currentChanges = changes;
                            changes = new List<FileChange>();
                        }
                        try
                        {
                            var ignored = new List<FileChange>();
                            foreach (var change in currentChanges)
                            {
                                if (change.Path.Contains(".git") || change.Path.Contains(".idea"))
                                {
                                    ignored.Add(change);
                                }
                            }
                            foreach (var change in ignored)
                            {
                                currentChanges.Remove(change);
                            }
                            ignored = null;

                            if (currentChanges.Count > 0)
                            {
                                Synchronize(currentChanges);
                            }
                        }
                        catch (Exception e)
                        {
                            SaveException(e);
                        }
                    }
                }
            }
        }

        public void StopWatching()
        {
            stopEvent.Set();
            foreach (var watcher in watchers)
            {
                watcher.EnableRaisingEvents = false;
            }
            watchers = new List<FileSystemWatcher>();
        }

        public void SaveException(Exception e)
        {
            var filename = String.Format(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                    + "\\m-sync\\logs\\{0:yy.MM.dd H.mm.ss}.txt", DateTime.Now);
            Directory.CreateDirectory(Path.GetDirectoryName(filename));
            File.WriteAllText(filename, String.Format("{0}\n{1}", e.Message, e.StackTrace));
        }

    }

    public class FileChange
    {
        public WatcherChangeTypes Type { get; set; }
        public string Path { get; set; }
    }
}
