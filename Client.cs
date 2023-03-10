using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DDNSClient;
public sealed class Client
{
    string User, Password, UserAgent, Hostname;
    string Ip, RetCode;
    readonly ILogger Logger;
    readonly HttpClient HttpClient = new();

    public Client(ILogger<WindowsBackgroundService> logger, IOptions<DDNSConfig> options)
    {
        Logger = logger;

        // unpack the config options
        User = options.Value.Username;
        Password = options.Value.Password;
        UserAgent = "DDNSClient/1.0 " + options.Value.Email;
        Hostname = options.Value.Hostname;

        // configure the HTTP client
        HttpClient.DefaultRequestHeaders.Add("Authorization", "Basic" + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{User}:{Password}")));
        HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);
        //HttpClient.DefaultRequestHeaders.UserAgent = userAgent;
    }

    public async Task<int> Initialize()
    {
        try
        {
            Ip = await GetCurrentIP();
            string _ddnsIp = Dns.GetHostAddresses(Hostname)[0]?.ToString(); // To avoid spamming requests when debugging
            if (_ddnsIp != Ip)
            {
                Logger.LogInformation("DNS record and current IP are different, initiating update.");
                PerformDNSUpdate();
            }
            else
            {
                Logger.LogInformation("DNS record and current IP are the same. Not performing init update.");
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError("HTTP Error: {ExMessage}", ex.Message);
            // TODO handle network error (not connected)
            Environment.Exit(2);
        }
        catch (SocketException ex) 
        {
            Logger.LogInformation("No existing DNS record for the give hostname. Exception: {ExMessage}", ex.Message);
            PerformDNSUpdate();
        }

        return 0;
    }

    public async void Update()
    {
        string ip = await GetCurrentIP();
        if (ip == Ip)
        {
            Logger.LogInformation("IP unchanged, waiting five minutes.");
        }
        else
        {
            Ip = ip;
            Logger.LogInformation("IP change detected. Updating DNS entry.");
            PerformDNSUpdate();
        }
    }

    /// <summary>
    /// Send a Dynamic DNS update, using the Ip field's value.
    /// </summary>
    async void PerformDNSUpdate()
    {
        // Google DNS update URI pattern
        // https: //username:password@domains.google.com/nic/update?hostname=subdomain.yourdomain.com&myip=1.2.3.4
        string reqURI = $"https://{User}:{Password}@domains.google.com/nic/update?hostname={Hostname}&myip={Ip}";
        RetCode = await HttpClient.GetStringAsync(reqURI);
        if (!RetCode.StartsWith("good") && !RetCode.StartsWith("nochg"))
        {
            Logger.LogError("Error with DNS update request: {RetCode} \nExiting...", RetCode);
            Environment.Exit(1);
        }
        else
        {
            Logger.LogInformation("DNS update successful. Return code: {RetCode}", RetCode);
        }
    }

    async Task<string> GetCurrentIP()
    {
        // TODO make ip detection service user configurable
        /* 
        // For using checkip service
        var res = await HttpClient.GetAsync("http://checkip.dyndns.com");
        res.EnsureSuccessStatusCode();
        string newIP = await res.Content.ReadAsStringAsync(); 
        return newIP.Split("IP Address: ")[1].Split("</body>")[0];
        */
        return await HttpClient.GetStringAsync("https://domains.google.com/checkip");
    }
}

public class DDNSConfig
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string Email { get; set; }
    public string Hostname { get; set; }
}
