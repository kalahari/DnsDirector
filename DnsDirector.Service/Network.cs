using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace DnsDirector.Service
{
    public class Network
    {
        private const int MAX_TRIES = 25;
        private static readonly ILog log = LogManager.GetLogger(typeof(Network));
        private readonly Config config;
        private bool changeActive = true;
        private Action<Exception> eventException;
        public List<IPAddress> DefaultServers { get; protected set; } = new List<IPAddress>();

        public Network(Config config, Action<Exception> eventException)
        {
            log.Debug("new Network()");
            this.config = config;
            this.eventException = eventException;
        }

        public async Task Start()
        {
            log.Debug("Start()");
            NetworkChange.NetworkAvailabilityChanged += Network_NetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged += Network_NetworkAddressChanged;
            changeActive = true;
            await PollInterfaces();
        }

        public void Stop()
        {
            log.Debug("Stop()");
            changeActive = false;
            NetworkChange.NetworkAvailabilityChanged -= Network_NetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged -= Network_NetworkAddressChanged;
            RevertInterfaces();
        }

        private void Network_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            log.Info("Network_NetworkAvailabilityChanged()");
            NetworkChanged();
        }

        private void Network_NetworkAddressChanged(object sender, EventArgs e)
        {
            log.Info("Network_NetworkAddressChanged()");
            NetworkChanged();
        }

        private void NetworkChanged()
        {
            if (!changeActive)
            {
                log.Info("Network change reaction disabled.");
                return;
            }
            try
            {
                PollInterfaces().Wait();
            }
            catch (Exception ex)
            {
                log.Error($"Unhandled exception on network availability change.", ex);
                eventException(ex);
            }
        }

        public async Task PollInterfaces()
        {
            log.Debug("PollInterfaces()");
            try
            {
                changeActive = false;
                if (!config.DhcpWithStaticDns) {
                    log.Info("Refreshing DHCP DNS resolvers");
                    var dhcpAdapters = new List<string>();
                    EachInterface(cfg =>
                    {
                        if (!cfg.DhcpEnabled)
                        {
                            log.Debug("Skipping interface, DHCP not enaled");
                            return;
                        }
                        if (!cfg.Resolvers.Any())
                        {
                            log.Debug("Skipping interface, no resolvers");
                            return;
                        }
                        log.Info($"Clearing resolvers on interface");
                        dhcpAdapters.Add(cfg.Description);
                        SetDnsResolvers(cfg.Adapter, null);
                    });
                    for (var i = 0; i < 100 && dhcpAdapters.Any(); i++)
                    {
                        log.Debug($"Reading DHCP resolvers for adapters: {string.Join(", ", dhcpAdapters)}");
                        await Task.Delay(TimeSpan.FromSeconds(0.1));
                        EachInterface(cfg =>
                        {
                            if (cfg.Resolvers.Any())
                            {
                                log.Info($"Mew DHCP resolvers: {string.Join(", ", cfg.Resolvers)}");
                                dhcpAdapters.Remove(cfg.Description);
                            }
                        });
                    }
                    if (dhcpAdapters.Any())
                    {
                        var ex = new NetworkException($"Unable to re-establish DHCP resolvers for adapters: {string.Join(", ", dhcpAdapters)}");
                        log.Error(null, ex);
                        throw ex;
                    }
                }
                var newServers = new List<IPAddress>();
                EachInterface(cfg =>
                {
                    if (cfg.Resolvers.Any())
                    {
                        var notLoopbackServers = cfg.Resolvers.Where(s => !s.Equals(IPAddress.Loopback));
                        var newDefaultServers = notLoopbackServers.Where(s => !newServers.Contains<IPAddress>(s));
                        log.Info($"Adding servers to default group: {string.Join(", ", newDefaultServers)}");
                        newServers.AddRange(newDefaultServers);
                        if (!cfg.Resolvers[0].Equals(IPAddress.Loopback))
                        {
                            ApplyLoopbackResolver(cfg.Adapter, notLoopbackServers);
                        }
                    }
                });
                DefaultServers = newServers;
            }
            finally
            {
                changeActive = true;
            }
        }

        public void RevertInterfaces()
        {
            log.Debug("RevertInterfaces()");
            EachInterface(cfg =>
            {
                if (cfg.Resolvers.Any() && cfg.Resolvers.First().Equals(IPAddress.Loopback))
                {
                    log.Info("Removing loopback as primary DNS server");
                    SetDnsResolvers(cfg.Adapter, cfg.DhcpEnabled && !config.DhcpWithStaticDns ? null : cfg.Resolvers.Skip(1));
                }
            });
        }

        private void ApplyLoopbackResolver(ManagementObject adapter, IEnumerable<IPAddress> notLoopbackServers)
        {
            log.Info("Adding loopback as primary DNS server");
            SetDnsResolvers(adapter, new List<IPAddress>() { IPAddress.Loopback }.Concat(notLoopbackServers));
        }

        internal void EachInterface(Action<AdapterConfig> handleInterface)
        {
            // https://msdn.microsoft.com/en-us/library/windows/desktop/aa394217%28v=vs.85%29.aspx
            var mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            var tries = 0;
            var keepTrying = true;
            while (keepTrying)
            {
                try
                {
                    var adapterConfigs = mc.GetInstances()
                        .Cast<ManagementObject>()
                        .Where(ac => (bool)ac["IPEnabled"])
                        .ToList();
                    log.Info($"Iterating {adapterConfigs.Count()} IP enabled interfaces");
                    foreach (var adapter in adapterConfigs)
                    {
                        var idx = (uint)adapter["Index"];
                        using (LogicalThreadContext.Stacks["NDC"].Push(idx.ToString()))
                        {
                            var cfg = new AdapterConfig();
                            cfg.Adapter = adapter;
                            cfg.Description = (string)adapter["Description"];
                            log.Info($"Description: {cfg.Description}");
                            var dnsServers = (string[])adapter["DNSServerSearchOrder"] ?? new string[0];
                            log.Debug($"DNS Servers: {string.Join(", ", dnsServers)}");
                            cfg.Resolvers = dnsServers.Select(s => IPAddress.Parse(s)).ToList();
                            cfg.DhcpEnabled = (bool)adapter["DHCPEnabled"];
                            log.Debug($"DHCP Enabled: {cfg.DhcpEnabled}");
                            handleInterface(cfg);
                        }
                    }
                    keepTrying = false;
                }
                catch (NetworkException ex)
                {
                    tries++;
                    if (tries > MAX_TRIES)
                    {
                        log.Error($"Exceeded {MAX_TRIES} tries on EachInterface(), failing.", ex);
                        throw;
                    }
                    log.Error($"Caught exception after {tries} tries on EachInterface(), will retry", ex);
                    Thread.Sleep(tries * 10);
                }
            }
        }

        internal void SetDnsResolvers(ManagementObject adapter, IEnumerable<IPAddress> resolvers)
        {
            try
            {
                log.Info($"Setting DNS resolvers: {(resolvers == null ? "null" : string.Join(", ", resolvers))}");
                var param = adapter.GetMethodParameters("SetDNSServerSearchOrder");
                param["DNSServerSearchOrder"] = resolvers == null ? null : resolvers
                    .Select(a => a.ToString())
                    .ToArray();
                var result = (uint)adapter.InvokeMethod("SetDNSServerSearchOrder", param, null)["ReturnValue"];
                log.Debug($"SetDNSServerSearchOrder got result: {result}");
                if (result != 0)
                {
                    var ex = new NetworkException($"SetDNSServerSearchOrder got result: {result}, expected: 0, see: https://msdn.microsoft.com/en-us/library/windows/desktop/aa393295");
                    ex.Data.Add("ReturnValue", result);
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error setting DNS servers", ex);
                throw;
            }
        }
    }

    public class AdapterConfig
    {
        public string Description { get; set; }
        public ManagementObject Adapter { get; set; }
        public List<IPAddress> Resolvers { get; set; }
        public bool DhcpEnabled { get; set; }
    }

    public class NetworkException : Exception
    {
        public NetworkException() : base() { }
        public NetworkException(string msg) : base(msg) { }
        public NetworkException(string msg, Exception inner) : base(msg, inner) { }
        public NetworkException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }
}
