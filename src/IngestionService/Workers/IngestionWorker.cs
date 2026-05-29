using IngestionService.Database;
using IngestionService.Messaging;
using Shared.Contracts.Messaging;

namespace IngestionService.Workers;

public sealed class IngestionWorker : BackgroundService
{
    private readonly RabbitMqConnectionFactory _factory;
    private readonly ReadingRepository _repository;
    private readonly DbInitializer _dbInitializer;
    private readonly ILogger<IngestionWorker> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public IngestionWorker(
        RabbitMqConnectionFactory factory,
        ReadingRepository repository,
        DbInitializer dbInitializer,
        ILogger<IngestionWorker> logger,
        ILoggerFactory loggerFactory)
    {
        _factory = factory;
        _repository = repository;
        _dbInitializer = dbInitializer;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _dbInitializer.InitializeAsync();

        await using var consumer = await ReadingsConsumer.CreateAsync(
            _factory,
            _repository,
            _loggerFactory.CreateLogger<ReadingsConsumer>());

        await consumer.StartConsumingAsync(stoppingToken);

        _logger.LogInformation("IngestionService running.");

        // Keep the worker alive — the consumer runs on RabbitMQ callbacks
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
