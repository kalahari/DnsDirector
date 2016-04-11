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

        public Network()
        {
            NetworkChange.NetworkAvailabilityChanged += Network_NetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged += Network_NetworkAddressChanged;
        }

        private void Network_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            PollInterfaces();
        }

        private void Network_NetworkAddressChanged(object sender, EventArgs e)
        {
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
            foreach (var adapter in adapterConfigs)
            {
                var idx = (uint)adapter["Index"];
                using (log4net.LogicalThreadContext.Stacks["NDC"].Push(idx.ToString("3")))
                {
                    log.Info($"Description: {(string)adapter["Description"]}");
                    var dnsServers = (string[])adapter["DNSServerSearchOrder"];
                    log.Debug($"DNS Servers: {string.Join(", ", dnsServers)}");
                }
            }
        }
    }
}
