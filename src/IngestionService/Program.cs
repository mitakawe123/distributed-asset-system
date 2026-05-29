using IngestionService.Database;
using IngestionService.Workers;
using Shared.Contracts.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<DbOptions>(
    builder.Configuration.GetSection(DbOptions.SectionName));

builder.Services.AddRabbitMq(builder.Configuration);

builder.Services.AddSingleton<DbInitializer>();
builder.Services.AddSingleton<ReadingRepository>();
builder.Services.AddHostedService<IngestionWorker>();

var host = builder.Build();
await host.RunAsync();