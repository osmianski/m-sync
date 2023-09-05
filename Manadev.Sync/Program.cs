using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceProcess;
using System.Diagnostics;

namespace Manadev.Sync.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("Starting m-sync");

            if (Global.Create().Get("debug") == "1")
            {
                Debugger.Launch();
            }
            if (args.Length > 0)
            {
                var command = args.Length > 0 ? args[0] : "project";
                Options.DeleteExistingFiles = args.Contains("--delete-existing-files");
                Options.HandleExceptions = !args.Contains("--show-exception-info");
                Program program = new Program();
                switch (command)
                {
                    case "set":
                        var key = args.Length > 1 ? args[1] : "";
                        if (key != "")
                        {
                            var global = Global.Create();
                            if (args.Length > 2)
                            {
                                global.Set(key, args[2]);
                            }
                            else
                            {
                                System.Console.WriteLine(String.Format("{0} = {1}", key, global.Get(key)));
                            }
                        }
                        else
                        {
                            System.Console.WriteLine("Usage: m-sync set <key> [<value>]");
                        }
                        break;
                    case "service-set":
                        key = args.Length > 1 ? args[1] : "";
                        if (key != "")
                        {
                            var global = Global.Create();
                            if (args.Length > 2)
                            {
                                global.ServiceSet(key, args[2]);
                            }
                            else
                            {
                                System.Console.WriteLine(String.Format("{0} = {1}", key, global.ServiceGet(key)));
                            }
                        }
                        else
                        {
                            System.Console.WriteLine("Usage: m-sync service-set <key> [<value>]");
                        }
                        break;
                    case "extension":
                        var extensionPath = args.Length > 1 ? args[1] : "";
                        if (extensionPath != "")
                        {
                            program.SyncronizeExtension(extensionPath);
                        }
                        else
                        {
                            System.Console.WriteLine("Usage: m-sync extension <relative extension path>");
                        }
                        break;
                    case "project": program.SyncronizeProject(); break;
                    case "all": program.SyncronizeAll(); break;
                    case "watch": program.StartWatching(); break;
                    case "start": program.StartService(); break;
                    case "stop": program.StopService(); break;
                    case "install": program.InstallService(args); break;
                    case "uninstall": program.UnnstallService(args); break;
                }
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] { new Service() };
                ServiceBase.Run(ServicesToRun);
            }
        }

        private void StopService()
        {
            throw new NotImplementedException();
        }

        private void UnnstallService(string[] args)
        {
            MyServiceInstaller.Install(true, args);
        }

        private void InstallService(string[] args)
        {
            MyServiceInstaller.Install(false, args); 
        }

        private void StartService()
        {
            throw new NotImplementedException();
        }

        private void StartWatching()
        {
            var global = Global.Create();
            global.Watch();
            System.Console.WriteLine("All extensions in all projects are being monitored for changes.");
            System.Console.WriteLine("Press any key to end file monitoring");
            System.Console.ReadKey();
        }

        private void SyncronizeAll()
        {
            var global = Global.Create();
            global.Synchronize();
            System.Console.WriteLine("All extensions in all projects are synchronized.");
        }

        private void SyncronizeProject()
        {
            Project project = null;
            if (Options.HandleExceptions)
            {
                try
                {
                    project = Project.Create(Environment.CurrentDirectory);
                    project.Synchronize();
                }
                catch (Exception e)
                {
                    System.Console.WriteLine(String.Format("Error: {0}", e.Message));
                    System.Console.ReadKey();
                    return;
                }
            }
            else {
                project = Project.Create(Environment.CurrentDirectory);
                project.Synchronize();
            }
            System.Console.WriteLine(String.Format("All extensions in project '{0}' are synchronized.",
                project.Path));
        }

        private void SyncronizeExtension(string extensionPath)
        {
            Project project = null;
            try
            {
                project = Project.Create(Environment.CurrentDirectory);
                var extension = Extension.Create(project, extensionPath);
                extension.Synchronize();
            }
            catch (Exception e)
            {
                System.Console.WriteLine(String.Format("Error: {0}", e.Message));
                System.Console.ReadKey();
                return;
            }
            System.Console.WriteLine(String.Format("Extension at '{0}' is synchronized with project '{1}'.", 
                extensionPath, project.Path));
        }
    }
}
