echo "Installing DnsDirector as a service."
cd "%~dp0"
"%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe" DnsDirector.Service.exe
sc failure "DnsDirector" reset= 300 actions= restart/3000/restart/3000/restart/3000
net start DnsDirector
timeout /t -1
