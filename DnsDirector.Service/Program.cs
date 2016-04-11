using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using System.IO;

namespace DnsDirector.Service
{
    static class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        private static readonly string log4netConfigFile = "DnsDirector.Service.log4net.xml";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            try
            {
                XmlConfigurator.ConfigureAndWatch(new FileInfo(log4netConfigFile));
            }
            catch (Exception ex)
            {
                // fall back to console
                BasicConfigurator.Configure();
                log.Error($"Unable to configure log4net with file: {log4netConfigFile}, falling back to console.", ex);
            }

            log.Debug($"Main({string.Join(", ", args.Select(arg => $"\"{arg}\""))})");

            try
            {
                var svc = new Service();
                //ServiceBase.Run(svc);
                svc.ConsoleStart();
            }
            catch (Exception ex)
            {
                log.Fatal("Uncaught top level exception.", ex);
                throw; // should this be here?
            }

            log.Debug("Main: void");
        }
    }
}
