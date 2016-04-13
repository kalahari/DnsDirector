echo "Installing DnsDirector as a service."
"%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe" DnsDirector.Service.exe
net start DnsDirector
timeout /t -1