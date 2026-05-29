using RabbitMQ.Client;
using Shared.Contracts.Messages;
using Shared.Contracts.Messaging;
using System.Text.Json;

namespace AssetService.Messaging;

public sealed class ReadingPublisher : IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<ReadingPublisher> _logger;

    private ReadingPublisher(
        IConnection connection,
        IChannel channel,
        ILogger<ReadingPublisher> logger)
    {
        _connection = connection;
        _channel = channel;
        _logger = logger;
    }

    public static async Task<ReadingPublisher> CreateAsync(
       RabbitMqConnectionFactory factory,
       ILogger<ReadingPublisher> logger)
    {
        var connection = await factory.CreateConnectionAsync("asset-publisher");
        var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: ExchangeNames.Readings,
            type: ExchangeType.Fanout,
            durable: true);

        return new ReadingPublisher(connection, channel, logger);
    }

    public async Task PublishAsync(AssetReading reading, CancellationToken ct = default)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(reading);

        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString(),
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
        };

        await _channel.BasicPublishAsync(
            exchange: ExchangeNames.Readings,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct);

        _logger.LogInformation(
            "Asset {AssetId} published reading: primary={Primary:F2} at {Timestamp}",
            reading.AssetId, reading.PrimaryValue, reading.Timestamp);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
    }
}
