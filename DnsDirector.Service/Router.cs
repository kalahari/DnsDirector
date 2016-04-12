using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DnsDirector.Service
{
    public class Router
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Server));
        private Config config;
        private Network network;

        public Router(Config config, Network network)
        {
            this.config = config;
            this.network = network;
        }

        public List<IPAddress> GetResolvers(string name)
        {
            name = name.ToLowerInvariant();
            log.Debug($"Getting resolvers for: {name}");
            var parts = name.Split('.').ToList();
            while (parts.Any())
            {
                log.Debug($"Testing name: ")
                var test = string.Join(".", parts);
                if (config.DnsRoutes.ContainsKey(test))
                {

                    return config.DnsRoutes[test];
                }
            }
            return null;
        }
    }
}
