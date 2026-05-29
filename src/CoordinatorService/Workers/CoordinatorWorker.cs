using CoordinatorService.Messaging;
using Shared.Contracts.Messaging;

namespace CoordinatorService.Workers;

public sealed class CoordinatorWorker(
    RabbitMqConnectionFactory factory,
    CommandPublisherAccessor accessor,
    ILogger<CoordinatorWorker> logger,
    ILoggerFactory loggerFactory) : IHostedService
{
    private readonly RabbitMqConnectionFactory _factory = factory;
    private readonly CommandPublisherAccessor _accessor = accessor;
    private readonly ILogger<CoordinatorWorker> _logger = logger;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    public async Task StartAsync(CancellationToken ct)
    {
        _logger.LogInformation("CoordinatorService starting.");

        var publisher = await CommandPublisher.CreateAsync(
            _factory,
            _loggerFactory.CreateLogger<CommandPublisher>());

        _accessor.Publisher = publisher;

        _logger.LogInformation("CommandPublisher ready.");
    }

    public async Task StopAsync(CancellationToken ct)
    {
        if (_accessor.Publisher is not null)
            await _accessor.Publisher.DisposeAsync();
    }
}