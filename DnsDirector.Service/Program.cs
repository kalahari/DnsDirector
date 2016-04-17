using log4net;
using log4net.Config;
using System;
using System.IO;
using System.Linq;
using System.ServiceProcess;

namespace DnsDirector.Service
{
    static class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        private static readonly string log4netConfigFile = "DnsDirector.Service.log4net.xml";

        public static bool IsService { get; private set; }
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            // make sure we aren't digging in the wrong place
            // http://haacked.com/archive/2004/06/29/current-directory-for-windows-service-is-not-what-you-expect.aspx/
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            ConfigureLog();

            log.Debug($"Main({string.Join(", ", args.Select(arg => $"\"{arg}\""))})");

            try
            {
                Run(args);
            }
            catch (Exception ex)
            {
                log.Fatal("Uncaught top level exception.", ex);
                Environment.Exit(-1);
            }

            log.Debug("Main: void");
        }

        private static void Run(string[] args)
        {
            IsService = false;
            var svc = new Service();
            if (args.Any())
            {
                HandleArgs(args, svc);
            }
            else
            {
                IsService = true;
                ServiceBase.Run(svc);
            }
        }

        private static void HandleArgs(string[] args, Service svc)
        {
            if (args.Length != 1)
            {
                Help();
                throw new ArgumentException($"Unexpected args: {string.Join(", ", args.Select(arg => $"\"{arg}\""))}");
            }
            else
            {
                Action<Exception> fatalError = ex =>
                {
                    log.Fatal("Fatal error", ex);
                    Environment.Exit(-1);
                };
                switch (args[0])
                {
                    case "--help":
                        Help();
                        break;
                    case "--console":
                        svc.ConsoleStart(fatalError).Wait();
                        break;
                    case "--reset":
                        svc.Reset(fatalError).Wait();
                        break;
                    default:
                        Help();
                        throw new ArgumentException($"Unexpected args: \"{args[0]}\"");
                }
            }
        }

        private static void ConfigureLog()
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
        }

        static void Help()
        {
            Console.WriteLine("DnsDirector Service");
            Console.WriteLine("Usage:");
            Console.WriteLine("\tDnsDirector.Service.exe --help");
            Console.WriteLine("\t\tShow this help message.");
            Console.WriteLine("\tDnsDirector.Service.exe --console");
            Console.WriteLine("\t\tRun in the foreground with a console.");
            Console.WriteLine("\tDnsDirector.Service.exe --reset");
            Console.WriteLine("\t\tReset any DnsDirector specific network settings.");
            Console.WriteLine("\tDnsDirector.Service.exe");
            Console.WriteLine("\t\tRun as a Windows service. (Not from command prompt.)");
        }
    }
}
