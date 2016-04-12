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
        private Config config;
        private Server server;
        private Network network;
        private Router router;

        public Service()
        {
            log.Debug("new Service()");
        }

        public void ConsoleStart()
        {
            log.Warn("ConsoleStart()");
            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                log.Info($"OnStart({(args == null ? "" : string.Join(", ", args.Select(arg => $"\"{arg}\"")))})");
                config = config ?? new Config();
                config.UpdateConfig();
                server = server ?? new Server();
                server.Start();
                network = network ?? new Network();
                network.PollInterfaces();
            }
            catch (Exception ex)
            {
                log.Fatal($"Error starting service", ex);
                StopService();
            }
        }

        protected override void OnStop()
        {
            log.InfoFormat("OnStop()");
            try
            {
                server.Stop();
            }
            catch (Exception ex)
            {
                log.Fatal("Exception in service stop, terminating!", ex);
                Environment.Exit(-1);
            }
        }

        private Task StopService()
        {
            return Task.Run(() =>
            {
                try
                {
                    Stop();
                }
                catch(Exception ex)
                {
                    log.Fatal("Exception stopping service, terminating!", ex);
                    Environment.Exit(-1);
                }
            });
        }
    }
}
