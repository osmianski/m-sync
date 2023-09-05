using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.ServiceProcess;
using System.Configuration.Install;
using System.Collections;

namespace Manadev.Sync.Console
{
    [RunInstaller(true)]
    public sealed class MyServiceInstallerProcess : ServiceProcessInstaller
    {
        public MyServiceInstallerProcess()
        {
            this.Account = ServiceAccount.LocalSystem;
        }
    }
    [RunInstaller(true)]
    public sealed class MyServiceInstaller : ServiceInstaller
    {
        public MyServiceInstaller()
        {
            this.Description = "MANAdev tool which monitors and updates Symbolic Links in PHP projects.";
            this.DisplayName = "m-sync";
            this.ServiceName = "m-sync";
            this.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
        }

        public static void Install(bool undo, string[] args)
        {
            try
            {
                System.Console.WriteLine(undo ? "uninstalling" : "installing");
                using (AssemblyInstaller inst = new AssemblyInstaller(typeof(Program).Assembly, args))
                {
                    IDictionary state = new Hashtable();
                    inst.UseNewContext = true;
                    try
                    {
                        if (undo)
                        {
                            inst.Uninstall(state);
                        }
                        else
                        {
                            inst.Install(state);
                            inst.Commit(state);
                        }
                    }
                    catch
                    {
                        try
                        {
                            inst.Rollback(state);
                        }
                        catch { }
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine(ex.Message);
            }
        }
    
        protected override void OnAfterInstall(System.Collections.IDictionary savedState)
        {
            base.OnAfterInstall(savedState);
            using (var serviceController = new ServiceController(this.ServiceName, Environment.MachineName))
                serviceController.Start();
        }
    }
}
