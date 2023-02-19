# DDNSClient
A light-weight Windows BackgroundService for updating Dynamic DNS records using Dyndns2 protocol.

Made originally for Google DNS's dynamic DNS service, but planned to be more configurable to support other Dyndns2 services.

## Installation
Download the latest release from the [releases page](/releases) and configure the service by editing the `appconfig.json` to include:
```json
{
  ...
    
  "DDNSConfig": {
    "Username": "asfdifugao",
    "Password": "0198461032",
    "Email": "email@example.com",
    "Hostname": "your.domainname.com"
  }
}
```


Then, install the service though an __admin__ shell using:
```
sc.exe create "DDNS Client Service" binbath="path/to/DDClient.exe"
sc.exe config "DDNS Client Service" start=auto
```

*Note: Installer script or MSI is planned for the project, and should make things a lot smoother.*

