namespace DDNSClient;

public class WindowsBackgroundService : BackgroundService
{
    private readonly Client _client;
    private readonly ILogger<WindowsBackgroundService> _logger;

    public WindowsBackgroundService(Client client, ILogger<WindowsBackgroundService> logger)
    {
        _logger = logger;
        _client = client;
        _client.Initialize().Wait();
        _logger.LogInformation("Service Started");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _client.Update();
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
