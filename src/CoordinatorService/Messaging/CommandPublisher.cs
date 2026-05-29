using RabbitMQ.Client;
using Shared.Contracts.Messages;
using Shared.Contracts.Messaging;
using System.Text.Json;

namespace CoordinatorService.Messaging;

public sealed class CommandPublisher : IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<CommandPublisher> _logger;

    private CommandPublisher(
        IConnection connection,
        IChannel channel,
        ILogger<CommandPublisher> logger)
    {
        _connection = connection;
        _channel = channel;
        _logger = logger;
    }

    public static CommandPublisher FromAccessor(CommandPublisherAccessor accessor)
        => accessor.Publisher
           ?? throw new InvalidOperationException(
               "CommandPublisher has not been initialized yet.");

    public static async Task<CommandPublisher> CreateAsync(
        RabbitMqConnectionFactory factory,
        ILogger<CommandPublisher> logger)
    {
        var connection = await factory.CreateConnectionAsync("coordinator-publisher");
        var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: ExchangeNames.Commands,
            type: ExchangeType.Direct,
            durable: true);

        return new CommandPublisher(connection, channel, logger);
    }

    public async Task PublishAsync(AssetCommand command, CancellationToken ct = default)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(command);

        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString(),
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
        };

        await _channel.BasicPublishAsync(
            exchange: ExchangeNames.Commands,
            routingKey: command.AssetId,   // direct exchange routes by assetId
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct);

        _logger.LogInformation(
            "Issued {CommandType} to asset {AssetId}",
            command.Type, command.AssetId);
    }

    public async Task PublishToAllAsync(
        IReadOnlyList<string> assetIds,
        CommandType commandType,
        IReadOnlyDictionary<string, string>? parameters = null,
        CancellationToken ct = default)
    {
        foreach (var assetId in assetIds)
        {
            var command = new AssetCommand(
                AssetId: assetId,
                Type: commandType,
                IssuedAt: DateTimeOffset.UtcNow,
                Parameters: parameters);

            await PublishAsync(command, ct);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
    }
}