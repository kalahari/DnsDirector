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
        public List<IPAddress> DefaultServers { get; protected set; } = new List<IPAddress>();
        //private Dictionary<uint, string> setDnsResultValues;

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
            log.Debug("PollInterfaces()");
            var newServers = new List<IPAddress>();
            EachInterface((adapter, dnsResolvers) =>
            {
                if (dnsResolvers.Any())
                {
                    var notLoopbackServers = dnsResolvers.Where(s => !s.Equals(IPAddress.Loopback));
                    var newDefaultServers = notLoopbackServers.Where(s => !newServers.Contains<IPAddress>(s));
                    log.Info($"Adding servers to default group: {string.Join(", ", newDefaultServers)}");
                    newServers.AddRange(newDefaultServers);
                    if (!dnsResolvers[0].Equals(IPAddress.Loopback))
                    {
                        ApplyLoopbackResolver(adapter, notLoopbackServers);
                    }
                }
            });
            DefaultServers = newServers;
        }

        public void RevertInterfaces()
        {
            log.Debug("RevertInterfaces()");
            EachInterface((adapter, dnsResolvers) =>
            {
                if (dnsResolvers.Any() && dnsResolvers.First().Equals(IPAddress.Loopback))
                {
                    log.Info("Removing loopback as primary DNS server");
                    SetDnsResolvers(adapter, dnsResolvers.Skip(1));
                }
            });
        }

        private void ApplyLoopbackResolver(ManagementObject adapter, IEnumerable<IPAddress> notLoopbackServers)
        {
            log.Info("Adding loopback as primary DNS server");
            SetDnsResolvers(adapter, new List<IPAddress>() { IPAddress.Loopback }.Concat(notLoopbackServers));
        }

        private void EachInterface(Action<ManagementObject, List<IPAddress>> handleInterface)
        {
            // https://msdn.microsoft.com/en-us/library/windows/desktop/aa394217%28v=vs.85%29.aspx
            var mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            //if (setDnsResultValues == null)
            //{
            //    var qdc = mc.Methods.Cast<MethodData>()
            //        .Single(md => md.Name == "SetDNSServerSearchOrder")
            //        .Qualifiers
            //        .Cast<QualifierData>();
            //    log.Debug($"SetDNSServerSearchOrder Qualifiers: {string.Join(", ", qdc.Select(q => q.Name))}");
            //    var keys = (string[])qdc.Single(qd => qd.Name == "ValueMap").Value;
            //    var vals = (string[])qdc.Single(qd => qd.Name == "Values").Value;
            //    setDnsResultValues = Enumerable.Range(0, keys.Length).ToDictionary(i => uint.Parse(keys[i]), i => vals[i]);
            //}
            var adapterConfigs = mc.GetInstances().Cast<ManagementObject>().Where(ac => (bool)ac["IPEnabled"]).ToList();
            log.Info($"Iterating {adapterConfigs.Count()} IP enabled interfaces");
            foreach (var adapter in adapterConfigs)
            {
                var idx = (uint)adapter["Index"];
                using (LogicalThreadContext.Stacks["NDC"].Push(idx.ToString()))
                {
                    log.Info($"Description: {(string)adapter["Description"]}");
                    var dnsServers = (string[])adapter["DNSServerSearchOrder"] ?? new string[0];
                    log.Debug($"DNS Servers: {string.Join(", ", dnsServers)}");
                    var serverAddreses = dnsServers.Select(s => IPAddress.Parse(s)).ToList();
                    handleInterface(adapter, serverAddreses);
                }
            }
        }

        private void SetDnsResolvers(ManagementObject adapter, IEnumerable<IPAddress> resolvers)
        {
            try
            {
                log.Info($"Setting DNS resolvers: {string.Join(", ", resolvers)}");
                var param = adapter.GetMethodParameters("SetDNSServerSearchOrder");
                param["DNSServerSearchOrder"] = resolvers
                    .Select(a => a.ToString())
                    .ToArray();
                var result = (uint)adapter.InvokeMethod("SetDNSServerSearchOrder", param, null)["ReturnValue"];
                log.Debug($"DNSServerSearchOrder got result: {result}");
                if(result != 0)
                {
                    var ex = new Exception($"DNSServerSearchOrder got result: {result}, expected: 0");
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
}
