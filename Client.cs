using Microsoft.Extensions.Options;
using System.Text;

namespace DDNSClient;
public sealed class Client
{
    string User, Password, UserAgent, Hostname;
    string? Ip;
    readonly ILogger Logger;
    readonly HttpClient HttpClient = new();
    Result LastResult;

    public Client(ILogger<Client> logger, IOptions<DDNSConfig> options)
    {
        Logger = logger;

        // unpack the config options
        User = options.Value.Username;
        Password = options.Value.Password;
        UserAgent = "DDNSClient/1.0 " + options.Value.Email;
        Hostname = options.Value.Hostname;

        // configure the HTTP client
        HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Basic" + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{User}:{Password}")));
        HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);
        //HttpClient.DefaultRequestHeaders.UserAgent = userAgent;
    }

    public async void Initialize()
    {
        try
        {
            Ip = await GetCurrentIP();
            PerformDNSUpdate(); // Force update on init to make sure everything works.
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError("HTTP Error: {ExMessage}", ex.Message);
            LastResult = Result.NetworkError;
        }
    }

    public async void Update()
    {
        string ip;
        try
        {
            if (Ip == null && LastResult == Result.NetworkError)
            {
                Initialize(); // reforce init, since it didn't occur first time due to network errors
            }
            else
            {
                ip = await GetCurrentIP();
                if (ip == Ip)
                {
                    Logger.LogInformation("IP unchanged, waiting five minutes.");
                    LastResult = Result.NoChange;
                }
                else
                {
                    Ip = ip;
                    Logger.LogInformation("IP change detected. Updating DNS entry.");
                    PerformDNSUpdate();
                }
            }

        }
        catch (HttpRequestException ex)
        {
            Logger.LogError("Error updating IP. {ExMessage}", ex.Message);
            LastResult = Result.NetworkError;
        }
    }

    /// <summary>
    /// Send a Dynamic DNS update, using the Ip field's value, and update the LastResult field.
    /// </summary>
    async void PerformDNSUpdate()
    {
        // Google DNS update URI pattern
        // https: //username:password@domains.google.com/nic/update?hostname=subdomain.yourdomain.com&myip=1.2.3.4
        string reqURI = $"https://{User}:{Password}@domains.google.com/nic/update?hostname={Hostname}&myip={Ip}";
        string retCode = await HttpClient.GetStringAsync(reqURI);

        if (retCode.StartsWith("good"))
        {
            Logger.LogInformation("DNS update successful. Return code: ", retCode);
            LastResult = Result.Success;
        }
        else if (retCode.StartsWith("nochg"))
        {
            Logger.LogInformation("No change in DNS record. Return code: ", retCode);
            LastResult = Result.NoChange;
        }
        else
        {
            Logger.LogError("Error with DNS update request: {retCode} \nExiting...", retCode);
            LastResult = Result.Failed;
            Environment.Exit(2); // Some inherent problem with DNS updates, that must be fixed before retrying.
        }
    }

    async Task<string> GetCurrentIP()
    {
        // TODO make ip detection service user configurable
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

enum Result
{
    Success,
    NoChange,
    Failed,
    NetworkError
}