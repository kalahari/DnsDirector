# DnsDirector
[![Build status](https://ci.appveyor.com/api/projects/status/dgbuyqn9e94ri105?svg=true)](https://ci.appveyor.com/project/kalahari/dnsdirector)

DIrect DNS queries to different servers by pattern matching the domain.
Inspired by [`/etc/router`](http://hints.macworld.com/article.php?story=2004062902195410) in Mac OS.

## Download
* The [releases page](https://github.com/kalahari/DnsDirector/releases) has ZIP archives of both source and binaries for each release.
* You can find ZIP archives of every build at [AppVeyor](https://ci.appveyor.com/project/kalahari/dnsdirector). First select the build configuration (Release or Debug) job and then press the 'Artifacts' button.

## Install
1. Install .NET Framework 4.6.1 if it is not installed already by Windows Update: https://www.microsoft.com/en-us/download/details.aspx?id=49981. (If you run the installer and your only options are "Repair" or "Remove" then you already have it installed.)
1. Extract the ZIP archive to your preferred install location. I like `C:\Tools\DnsDirector`.
1. Windows might not trust the executable by default because it is not code signed, and you may need to unblock it.
  1. Right click on `DnsDirector.Service.exe` and select 'Properties'. ![Properties](https://raw.githubusercontent.com/wiki/kalahari/DnsDirector/img/DnsDirector_Unblock_1.png)
  1. Press the 'Unblock' button, and then press 'OK'. **If you don't see the 'Unblock' button, just press 'OK'.** ![Unblock](https://raw.githubusercontent.com/wiki/kalahari/DnsDirector/img/DnsDirector_Unblock_2.png)
1. Now run the service install command script as administrator. (The install script only works if you run it from the DnsDirector directory. So if you are installing from an admin command prompt, be sure to `cd` to that directory first.) ![Install](https://raw.githubusercontent.com/wiki/kalahari/DnsDirector/img/DnsDirector_Install.png)

## Upgrade
1. Open Administrative Tools -> Services and stop the DnsDirector service, or: `net stop DnsDirector`.
1. Rename the DnsDirector install directory.
1. Extract the new version of DnsDirector to a directory with exactly the same path as the old one you just renamed.
1. Copy config settings in `DnsDirectorRoutes.yml` from the old install to the new. Be aware that the new config file may have new settings not present in the old one.
1. Start the DnsDirector service.
1. Remove the old direcctory once you have confimred everything is working as expected.

## Configuration
All configuration is in `DnsDirectorRoutes.yaml`

```yaml
# Configuration for the DnsDirector service

# If your network is configured automatically,
# but your DNS servers are configured manually,
# set this option to 'true'. This would be an
# unusual configuration. DnsDirector is unable
# to detect this situation on its own.
dhcpWithStaticDns: false

# Use a widely know set of public DNS servers
# handle all requests not matched in routes.
# Current list is servers from Google and Level 3.
# Only set this to 'true' if you don't want to
# use your system configured DNS servers at all.
usePublicDefaultServers: false

# Provide your own list of DNS server to
# handle all requests not matched in routes.
# Only define this if you don't want to use
# your system configured DNS servers at all.
#defaultServers:
#  - '8.8.8.8'
#  - '8.8.4.4'

# Override the DNS server used by domain suffix.
# Will only match whole parts of the domain suffx,
# e.g. a query for the name 'foobar.example.com'
# does not match a route for 'bar.example.com', but
# it does match a route for 'example.com'.
# Records are matched from most specific to least,
# the order in this file is not relevant.
routes:
  bar.example.com: ['4.2.2.1', '4.2.2.2']
  example.com: ['4.2.2.3', '4.2.2.4']
```

### Log Configuration
You can edit the logging configuration in `DnsDirector.Service.log4net.xml`,
see https://logging.apache.org/log4net/release/config-examples.html for more information.

## License
Copyright 2016 Blake Mitchell &lt;blake@barkingspoon.com&gt;

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
