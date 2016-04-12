using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DnsDirector.Service
{
    public class Config
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Config));
        private static readonly string configFile = "DnsDirectorRoutes.yaml";
        private static readonly IList<IPAddress> publicDnsServers = new List<IPAddress>()
        {
            IPAddress.Parse("4.2.2.4"), // Level 3
            IPAddress.Parse("8.8.4.4"), // Google
            IPAddress.Parse("4.2.2.3"), // Level 3
            IPAddress.Parse("4.2.2.2"), // Level 3
            IPAddress.Parse("8.8.8.8"), // Google
            IPAddress.Parse("4.2.2.1"), // Level 3
        }.AsReadOnly();

        private readonly FileSystemWatcher configFileWatcher;
        private bool configured = false;

        public bool UsePublicDefaultServers { get; protected set; } = false;
        public List<IPAddress> DefaultDnsServers { get; protected set; } = new List<IPAddress>();
        public Dictionary<string, List<IPAddress>> DnsRoutes { get; protected set; } = new Dictionary<string, List<IPAddress>>();

        public Config()
        {
            configFileWatcher = new FileSystemWatcher(".", configFile);
            configFileWatcher.Changed += ConfigFileWatcher_Changed;
            configFileWatcher.EnableRaisingEvents = true;
        }

        private void ConfigFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            log.Debug($"Config watcher got event: {e.ChangeType} for: {e.Name}");
            if (e.Name == configFile && (e.ChangeType == WatcherChangeTypes.Created || e.ChangeType == WatcherChangeTypes.Changed))
            {
                UpdateConfig();
            }

        }

        public void UpdateConfig()
        {
            log.Info($"Updating config from file: {configFile}");
            var changed = false;
            try
            {
                var configFileInfo = new FileInfo(configFile);
                ConfigData data;
                using(var reader = configFileInfo.OpenRead())
                {
                    var dezer = new Deserializer(namingConvention: new CamelCaseNamingConvention());
                    data = dezer.Deserialize<ConfigData>(new StreamReader(reader));
                }
                log.Debug("Read new config from file");

                log.Info($"Use public default servers: {data.UsePublicDefaultServers} (was: {UsePublicDefaultServers})");
                var newDefaultServers = new List<IPAddress>();
                data.DefaultServers = data.DefaultServers ?? new List<string>();
                log.Info($"Default servers: {string.Join(", ", data.DefaultServers)} (was: {string.Join(", ", DefaultDnsServers)})");
                newDefaultServers.AddRange(data.DefaultServers.Select(s => IPAddress.Parse(s)));
                var newRoutes = new Dictionary<string, List<IPAddress>>();
                foreach(var key in data.Routes.Keys)
                {
                    log.Info($"Servers for: {key} [{string.Join(", ", data.Routes[key])}]");
                    newRoutes.Add(key.ToLowerInvariant(), data.Routes[key].Select(s => IPAddress.Parse(s)).ToList());
                }

                UsePublicDefaultServers = data.UsePublicDefaultServers;
                changed = true;
                DefaultDnsServers = newDefaultServers;
                
                configured = true;
            }
            catch (Exception ex)
            {
                log.Error($"Error updating config from file, config {(changed ? "partially" : "not")} changed", ex);
                if (!configured) throw;
            }
        }

        class ConfigData
        {
            public bool UsePublicDefaultServers { get; set; }
            public List<string> DefaultServers { get; set; }
            public Dictionary<string, List<string>> Routes { get; set; }
        }
    }
}
