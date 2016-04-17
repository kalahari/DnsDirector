using log4net;
using System.Collections.Generic;
using System.Linq;
using System.Net;

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

        public IList<IPAddress> GetResolvers(string name)
        {
            name = name.ToLowerInvariant().Trim('.');
            using (LogicalThreadContext.Stacks["NDC"].Push(name))
            {
                log.Debug($"Getting resolvers");
                var parts = name.Split('.').AsEnumerable();
                while (parts.Any())
                {
                    var test = string.Join(".", parts);
                    log.Debug($"Testing name: {test}");
                    if (config.DnsRoutes.ContainsKey(test))
                    {
                        var resolvers = config.DnsRoutes[test];
                        log.Debug($"Found resolvers for: {test} [{string.Join(", ", resolvers)}]");
                        return resolvers;
                    }
                    parts = parts.Skip(1);
                }
                log.Debug("No matching route");
                if (config.UsePublicDefaultServers)
                {
                    log.Debug($"Using public default resolvers: [{string.Join(", ", Config.PublicDnsServers)}]");
                    return Config.PublicDnsServers;
                }
                if (config.DefaultDnsServers.Any())
                {
                    log.Debug($"Using config default resolvers: [{string.Join(", ", config.DefaultDnsServers)}]");
                    return config.DefaultDnsServers;
                }
                if (network.DefaultServers.Any())
                {
                    log.Debug($"Using system default resolvers: [{string.Join(", ", network.DefaultServers)}]");
                    return network.DefaultServers;
                }
                log.Debug($"No system/config default resolvers found, using public default resolvers: [{string.Join(", ", Config.PublicDnsServers)}]");
                return Config.PublicDnsServers;
            }
        }
    }
}
