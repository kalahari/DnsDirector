using log4net;
using System;
using System.Linq;
using System.ServiceProcess;
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

        public async Task ConsoleStart(Action<Exception> fatalError)
        {
            log.Warn("ConsoleStart()");
            await Startup(fatalError);
            Console.WriteLine("Press [Enter] key to exit.");
            await Task.Run(() =>
            {
                Console.ReadLine();
                log.Warn("Got line on console.");
            });
            Shutdown();
            log.Warn("ConsoleStart: void");
        }

        internal async Task Reset(Action<Exception> fatalError)
        {
            log.Warn("Reset()");
            var config = new Config();
            config.UpdateConfig();
            var net = new Network(config, fatalError);
            net.EachInterface(cfg =>
            {
                if (cfg.DhcpEnabled)
                {
                    net.SetDnsResolvers(cfg.Adapter, null);
                }
            });
            Console.WriteLine("Press [Enter] key to exit.");
            await Task.Run(() =>
            {
                Console.ReadLine();
                log.Warn("Got line on console.");
            });
            log.Warn("ConsoleStart: void");
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                log.Info($"OnStart({(args == null ? "" : string.Join(", ", args.Select(arg => $"\"{arg}\"")))})");
                base.OnStart(args);
                Startup(ex =>
                {
                    log.Fatal("Fatal exception on service event", ex);
                    StopService();
                }).Wait();
            }
            catch (Exception ex)
            {
                log.Fatal($"Error starting service", ex);
                StopService();
            }
        }

        private async Task Startup(Action<Exception> fatalError)
        {
            log.Debug("Startup()");
            config = config ?? new Config();
            network = network ?? new Network(config, fatalError);
            router = router ?? new Router(config, network);
            server = server ?? new Server(router);
            config.UpdateConfig();
            server.Start();
            await network.Start();
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
            network.Stop();
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
