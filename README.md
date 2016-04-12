# DnsDirector
DIrect DNS queries to different servers by pattern matching the domain

## Configuration
All configuration is in `DnsDirectorRoutes.yaml`

```yaml
# Configuration for the DnsDirector service

# Use a widely know set of public DNS servers
# handle all requests not matched in routes.
# Current list is servers from Google and Level 3.
# Only set this to 'true' if you don't want to
# use your system confgiured DNS servers at all.
usePublicDefaultServers: false

# Provide your own list of DNS server to
# handle all requests not matched in routes.
# Only define this if you don't want to use
# your system confgiured DNS servers at all.
# defaultServers:
#   - '8.8.8.8'
#   - '8.8.4.4'

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
