echo "Installing DnsDirector as a service."
net stop DnsDirector
"%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe" /u DnsDirector.Service.exe
timeout /t -1