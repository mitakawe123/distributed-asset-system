using AssetService.Messaging;
using Microsoft.Extensions.Options;
using Shared.Contracts.Messages;
using Shared.Contracts.Messaging;

namespace AssetService.Workers;

public sealed class AssetWorker(
    RabbitMqConnectionFactory factory,
    IOptions<AssetOptions> options,
    ILogger<AssetWorker> logger,
    ILoggerFactory loggerFactory) : BackgroundService
{
    private readonly RabbitMqConnectionFactory _factory = factory;
    private readonly IOptions<AssetOptions> _options = options;
    private readonly ILogger<AssetWorker> _logger = logger;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    private readonly Random _rng = new(options.Value.Id.GetHashCode());

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = _options.Value;

        _logger.LogInformation(
            "Asset {AssetId} starting — reporting every {Interval}s",
            opts.Id, opts.ReportingIntervalSecs);

        await using var publisher = await ReadingPublisher.CreateAsync(
            _factory,
            _loggerFactory.CreateLogger<ReadingPublisher>());

        await using var consumer = await CommandConsumer.CreateAsync(
            _factory,
            _options,
            _loggerFactory.CreateLogger<CommandConsumer>());

        await consumer.StartConsumingAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var reading = new AssetReading(
                AssetId: opts.Id,
                Timestamp: DateTimeOffset.UtcNow,
                PrimaryValue: Math.Round(_rng.NextDouble() * 100, 2),
                SecondaryValue: Math.Round(_rng.NextDouble() * 10, 2));

            await publisher.PublishAsync(reading, stoppingToken);

            await Task.Delay(
                TimeSpan.FromSeconds(opts.ReportingIntervalSecs),
                stoppingToken);
        }

        _logger.LogInformation("Asset {AssetId} stopping.", opts.Id);
    }
}
