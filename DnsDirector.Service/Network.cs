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
        private NetworkState state;

        public Network()
        {
            state = new NetworkState();
            MonitorChanges();
        }

        private void MonitorChanges()
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
            var nics = NetworkInterface.GetAllNetworkInterfaces();
            var available = NetworkInterface.GetIsNetworkAvailable();
            log.Info($"Polling {nics.Count()} interfaces, network available: {available}");
            var newState = state.Clone();
            var stateDirty = false;
            if(available != newState.NetworkAvailable)
            {
                log.Info($"Network availability changed from: {newState.NetworkAvailable} to: {available}");
                newState.NetworkAvailable = available;
                stateDirty = true;
            }
            var stateIds = newState.Interfaces.Keys.ToList();

                //.Where(nic => nic.OperationalStatus == OperationalStatus.Up
                //    && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback
                //    && !nic.IsReceiveOnly);
            foreach (var nic in nics)
            {
                var id = nic.Id;
                using (log4net.LogicalThreadContext.Stacks["NDC"].Push(id))
                {
                    var name = nic.Description;
                    var status = nic.OperationalStatus;
                    var type = nic.NetworkInterfaceType;
                    var receiveOnly = nic.IsReceiveOnly;
                    var dns = nic.GetIPProperties().DnsAddresses.ToList();
                    if (!stateIds.Contains(id))
                    {
                        log.Info($"New interface, name: {name}, status: {status}, type: {type}, receive only: {receiveOnly}, dns servers: {string.Join(", ", dns)}");
                        newState.Interfaces.Add(nic.Id, new InterfaceState()
                        {
                            Id = id,
                            Name = name,
                            Status = status,
                            Type = type,
                            ReceiveOnly = receiveOnly,
                            DnsServers = dns,
                        });
                        stateDirty = true;
                        continue;
                    }
                    stateIds.Remove(id);
                    var stateNic = newState.Interfaces[id];
                    if (stateNic.Name != name)
                    {
                        log.Info($"New name: {name}");
                        stateNic.Name = name;
                        stateDirty = true;
                    }
                    if (stateNic.Status != status)
                    {
                        log.Info($"New status: {status}");
                        stateNic.Status = status;
                        stateDirty = true;
                    }
                    if (stateNic.Type != type)
                    {
                        log.Info($"New type: {type}");
                        stateNic.Type = type;
                        stateDirty = true;
                    }
                    if (stateNic.ReceiveOnly != receiveOnly)
                    {
                        log.Info($"New receive only: {receiveOnly}");
                        stateNic.ReceiveOnly = receiveOnly;
                        stateDirty = true;
                    }
                    if (dns.Count == 1 && dns[0].Equals(IPAddress.Loopback))
                    {
                        log.Debug("Inverface using loopback for DNS");
                        continue;
                    }
                    if(!dns.Any() && stateNic.DnsServers.Any())
                    {
                        log.Info($"DNS servers removed: {string.Join(", ", stateNic.DnsServers)}");
                        stateNic.DnsServers = new List<IPAddress>();
                        stateDirty = true;
                        continue;
                    }
                }
            }
            foreach (var extra in stateIds)
            {
                log.Info($"Removed interface: {extra}");
                newState.Interfaces.Remove(extra);
                stateDirty = true;
            }
            if (stateDirty)
            {
                log.Info("Updating network state");
                state = newState;
            }

            var adapters = new ManagementClass("Win32_NetworkAdapter").GetInstances();
            var adapterConfigs = new ManagementClass("Win32_NetworkAdapterConfiguration").GetInstances();
            foreach (var adapter in adapters)
            {
                var guid = (adapter["GUID"] ?? "").ToString();
                var caption = (adapter["Caption"] ?? "").ToString();
                log.Info($"Win32_NetworkAdapter: {guid} {caption}");
                foreach (var config in adapterConfigs)
                {
                    var configCaption = (config["Caption"] ?? "").ToString();
                    if (caption == configCaption)
                    {
                        log.Info($"Win32_NetworkAdapterConfiguration: {config["IPEnabled"]}");
                    }
                }
            }
        }
    }
}
