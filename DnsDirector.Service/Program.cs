using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using log4net;
using log4net.Config;

namespace DnsDirector.Service
{
    static class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            BasicConfigurator.Configure();
            log.Debug($"Main({string.Join(", ", args.Select(arg => $"\"{arg}\""))})");
            var svc = new Service();
            //ServiceBase.Run(svc);
            svc.ConsoleStart();
            log.Debug("Main: void");
        }
    }
}
