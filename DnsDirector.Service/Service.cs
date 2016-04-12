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
            Startup();
            Console.WriteLine("Press [Enter] key to exit.");
            Task.Run(() =>
            {
                Console.ReadLine();
                log.Warn("Got line on console.");
            }).Wait();
            Shutdown();
            log.Warn("ConsoleStart: void");
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                log.Info($"OnStart({(args == null ? "" : string.Join(", ", args.Select(arg => $"\"{arg}\"")))})");
                base.OnStart(args);
                Startup();
            }
            catch (Exception ex)
            {
                log.Fatal($"Error starting service", ex);
                StopService();
            }
        }

        private void Startup()
        {
            log.Debug("Startup()");
            config = config ?? new Config();
            network = network ?? new Network();
            router = router ?? new Router(config, network);
            server = server ?? new Server(router);
            config.UpdateConfig();
            server.Start();
            network.PollInterfaces();
        }

        protected override void OnStop()
        {
            log.Info("OnStop()");
            try
            {
                Shutdown();
                base.OnStop();
            }
            catch (Exception ex)
            {
                log.Fatal("Exception in service stop, terminating!", ex);
                Environment.Exit(-1);
            }
        }

        private void Shutdown()
        {
            log.Debug("Shutdown()");
            network.RevertInterfaces();
            server.Stop();
        }

        private Task StopService()
        {
            log.Debug("StopService()");
            return Task.Run(() =>
            {
                if (!Program.IsService) return;
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
