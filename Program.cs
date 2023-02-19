using DDNSClient;

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
    .Build();

host.Run();
