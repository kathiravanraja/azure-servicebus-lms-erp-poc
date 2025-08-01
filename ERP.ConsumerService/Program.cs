using ERP.ConsumerService;
using ERP.ConsumerService.Services;
using ServiceBus.Shared.Configuration;
using ServiceBus.Shared.Services;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

// Add configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);
builder.Configuration.AddEnvironmentVariables();

// Configure ServiceBus settings
builder.Services.Configure<ServiceBusConfiguration>(
    builder.Configuration.GetSection(ServiceBusConfiguration.SectionName));

// Register services
builder.Services.AddSingleton<IServiceBusConsumer, ServiceBusConsumer>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<ServiceBusConsumer>>();
    var config = provider.GetRequiredService<IOptions<ServiceBusConfiguration>>();
    return new ServiceBusConsumer(config.Value.ConnectionString, logger);
});

builder.Services.AddSingleton<IErpGradeProcessor, ErpGradeProcessor>();

// Add the background service
builder.Services.AddHostedService<Worker>();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

// Enable Windows Service hosting if running on Windows
if (OperatingSystem.IsWindows())
{
    builder.Services.AddWindowsService();
}

var host = builder.Build();

try
{
    await host.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Application terminated unexpectedly: {ex.Message}");
    throw;
}
