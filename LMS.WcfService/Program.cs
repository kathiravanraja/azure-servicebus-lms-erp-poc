using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Description;
using LMS.WcfService.Contracts;
using LMS.WcfService.Services;
using ServiceBus.Shared.Configuration;
using ServiceBus.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);
builder.Configuration.AddEnvironmentVariables();

// Configure ServiceBus settings
builder.Services.Configure<ServiceBusConfiguration>(
    builder.Configuration.GetSection(ServiceBusConfiguration.SectionName));

// Register services
var serviceBusConfig = builder.Configuration.GetSection(ServiceBusConfiguration.SectionName).Get<ServiceBusConfiguration>();
if (serviceBusConfig == null)
{
    throw new InvalidOperationException("ServiceBus configuration is required");
}

builder.Services.AddSingleton<IServiceBusPublisher>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<ServiceBusPublisher>>();
    return new ServiceBusPublisher(serviceBusConfig.ConnectionString, logger);
});

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

// Add CoreWCF services
builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddSingleton<IGradingService, GradingService>();

var app = builder.Build();

// Configure CoreWCF
app.UseServiceModel(builder =>
{
    builder.AddService<GradingService>()
           .AddServiceEndpoint<GradingService, IGradingService>(new BasicHttpBinding(), "/GradingService.svc");
});

// Add metadata endpoint
var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
serviceMetadataBehavior.HttpGetEnabled = true;

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();

// Add a simple health check endpoint
app.MapGet("/health", async (IGradingService gradingService) =>
{
    var health = await gradingService.GetServiceHealthAsync();
    return Results.Ok(health);
});

app.MapGet("/", () => "LMS WCF Service is running. Service endpoint: /GradingService.svc");

app.Run();
