using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceProcess;
using System.IO;

namespace Manadev.Sync.Console
{
    public partial class Service : ServiceBase
    {
        public Service()
        {
            this.ServiceName = "m-sync";
        }

        protected override void OnStart(string[] args)
        {
            var global = Global.Create();
            try
            {
                global.Watch();
            }
            catch (Exception e)
            {
                global.SaveException(e);
            }
        }

        protected override void OnStop()
        {
            var global = Global.Create();
            try 
            {
                global.StopWatching();
            }
            catch (Exception e)
            {
                global.SaveException(e);
            }
        }
    }
}
