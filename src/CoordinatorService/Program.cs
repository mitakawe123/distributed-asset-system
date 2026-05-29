using CoordinatorService;
using CoordinatorService.Messaging;
using CoordinatorService.Registry;
using CoordinatorService.Workers;
using Shared.Contracts.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<CoordinatorOptions>(
    builder.Configuration.GetSection(CoordinatorOptions.SectionName));

builder.Services.AddRabbitMq(builder.Configuration);

builder.Services.AddSingleton<CommandPublisherAccessor>();
builder.Services.AddSingleton<AssetRegistry>();

builder.Services.AddHostedService<CoordinatorWorker>();

var opts = builder.Configuration
    .GetSection(CoordinatorOptions.SectionName)
    .Get<CoordinatorOptions>()!;

builder.Services.AddQ(q =>
{
    q.AddJob<CalibrateAllAssetsJob>(j => j.WithIdentity("calibrate-all"));
    q.AddTrigger(t => t
        .ForJob("calibrate-all")
        .WithCronSchedule(opts.CalibrateCron));

    q.AddJob<DiagnosticCheckJob>(j => j.WithIdentity("diagnostic-check"));
    q.AddTrigger(t => t
        .ForJob("diagnostic-check")
        .WithCronSchedule(opts.DiagnosticCron));
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var host = builder.Build();
await host.RunAsync();
