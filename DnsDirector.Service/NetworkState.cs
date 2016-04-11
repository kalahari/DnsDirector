using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace DnsDirector.Service
{
    public class NetworkState
    {
        public bool NetworkAvailable { get; set; }
        public Dictionary<string, InterfaceState> Interfaces { get; set; }

        public NetworkState()
        {
            Interfaces = new Dictionary<string, InterfaceState>();
        }

        public NetworkState Clone()
        {
            var ns = new NetworkState();
            ns.NetworkAvailable = NetworkAvailable;
            ns.Interfaces = Interfaces.ToDictionary(i => i.Key, i => i.Value.Clone());
            return ns;
        }
    }
    public class InterfaceState
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public OperationalStatus Status { get; set; }
        public NetworkInterfaceType Type { get; set; }
        public bool ReceiveOnly { get; set; }
        public List<IPAddress> DnsServers { get; set; }

        public InterfaceState Clone()
        {
            var i = new InterfaceState();
            i.Id = Id;
            i.Name = Name;
            i.Status = Status;
            i.Type = Type;
            i.ReceiveOnly = ReceiveOnly;
            i.DnsServers = DnsServers.Select(ds => new IPAddress(ds.GetAddressBytes())).ToList();
            return i;
        }
    }
}
