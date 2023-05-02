using DDNSClient;
using Microsoft.Extensions.Logging;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(option =>
    {
        option.ServiceName = "DDNS Client Service";
    })
    .ConfigureServices((hostContext, services ) =>
    {
        services.Configure<DDNSConfig>(hostContext.Configuration.GetSection(nameof(DDNSConfig)));
        services.AddSingleton<Client>();
        services.AddHostedService<WindowsBackgroundService>();
    })
    .ConfigureLogging((hostContext, logging) =>
    {
        logging.AddEventLog();
        logging.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
    })
    .Build();

host.Run();
