using AssetService;
using AssetService.Workers;
using Shared.Contracts.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOptions<AssetOptions>()
    .BindConfiguration(AssetOptions.SectionName)
    .ValidateOnStart();

builder.Services.AddRabbitMq(builder.Configuration);
builder.Services.AddHostedService<AssetWorker>();

var host = builder.Build();
await host.RunAsync();
