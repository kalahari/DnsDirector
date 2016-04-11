using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace DnsDirector.Service
{
    public class Service : ServiceBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Service));
        private readonly Server server;
        private readonly Network network;

        public Service()
        {
            log.Debug("new Service()");
            server = new Server();
            network = new Network();
        }

        public void ConsoleStart()
        {
            log.Warn("ConsoleStart()");
            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            log.Info($"OnStart({(args == null ? "" : string.Join(", ", args.Select(arg => $"\"{arg}\"")))})");
            server.Start();
            network.PollInterfaces();
        }

        protected override void OnStop()
        {
            log.InfoFormat("OnStop()");
            server.Stop();
        }
    }
}
