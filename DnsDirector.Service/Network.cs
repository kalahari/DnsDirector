using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using log4net;
using System.Net;
using System.Management;

namespace DnsDirector.Service
{
    public class Network
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Network));
        private List<IPAddress> defaultServers = new List<IPAddress>();

        public Network()
        {
            log.Debug("Network()");
            NetworkChange.NetworkAvailabilityChanged += Network_NetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged += Network_NetworkAddressChanged;
        }

        private void Network_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            log.Debug("Network_NetworkAvailabilityChanged()");
            PollInterfaces();
        }

        private void Network_NetworkAddressChanged(object sender, EventArgs e)
        {
            log.Debug("Network_NetworkAddressChanged()");
            PollInterfaces();
        }

        public void PollInterfaces()
        {
            // https://msdn.microsoft.com/en-us/library/windows/desktop/aa394217%28v=vs.85%29.aspx
            var adapterConfigs = new ManagementClass("Win32_NetworkAdapterConfiguration")
                .GetInstances()
                .Cast<ManagementObject>()
                .Where(ac => (bool)ac["IPEnabled"])
                .ToList();
            log.Info($"Polling {adapterConfigs.Count()} IP enabled interfaces");
            var newServers = new List<IPAddress>();
            foreach (var adapter in adapterConfigs)
            {
                PollAdapter(adapter, newServers);
            }
            defaultServers = newServers;
        }

        private static void PollAdapter(ManagementObject adapter, List<IPAddress> newServers)
        {
            var idx = (uint)adapter["Index"];
            using (LogicalThreadContext.Stacks["NDC"].Push(idx.ToString("3")))
            {
                log.Info($"Description: {(string)adapter["Description"]}");
                var dnsServers = (string[])adapter["DNSServerSearchOrder"];
                log.Debug($"DNS Servers: {string.Join(", ", dnsServers)}");
                var serverAddreses = dnsServers.Select(s => IPAddress.Parse(s)).ToList();
                if (serverAddreses.Any())
                {
                    var notLoopbackServers = serverAddreses.Where(s => !s.Equals(IPAddress.Loopback));
                    log.Info($"Adding servers to default group: {string.Join(", ", notLoopbackServers)}");
                    newServers.AddRange(notLoopbackServers);
                    if (!serverAddreses[0].Equals(IPAddress.Loopback))
                    {
                        //ApplyLoopbackResolver(adapter, notLoopbackServers);
                    }
                }
            }
        }

        private static void ApplyLoopbackResolver(ManagementObject adapter, IEnumerable<IPAddress> notLoopbackServers)
        {
            log.Info("Adding loopback as primary DNS server");
            try
            {
                var param = adapter.GetMethodParameters("SetDNSServerSearchOrder");
                param["DNSServerSearchOrder"] = new List<IPAddress>() { IPAddress.Loopback }
                    .Concat(notLoopbackServers)
                    .Select(a => a.ToString())
                    .ToArray();
                adapter.InvokeMethod("SetDNSServerSearchOrder", param, null);
            }
            catch (Exception ex)
            {
                log.Error($"Error setting DNS servers", ex);
                throw;
            }
        }
    }
}
