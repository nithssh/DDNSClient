using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace DDNSClient;
public sealed class Client
{
    string User, Password, UserAgent, Hostname;
    string Ip, RetCode;
    readonly ILogger Logger;
    readonly HttpClient HttpClient = new();

    public Client(ILogger<Client> logger, IOptions<DDNSConfig> options)
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

    public async void Initialize()
    {
        try
        {
            Ip = await GetCurrentIP();
            PerformDNSUpdate();
        } catch (HttpRequestException ex)
        {
            Logger.LogError("HTTP Error: {ExMessage}", ex.Message);
            // TODO handle network error (not connected)
            Environment.Exit(2);
        }
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
            Logger.LogError("Error with DNS update request: {Error} \nExiting...", RetCode);
            Environment.Exit(1);
        }
        else
        {
            Logger.LogInformation("DNS update successful. Return code: ", RetCode);
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
