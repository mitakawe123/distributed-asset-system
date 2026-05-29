using IngestionService.Database;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Messages;
using Shared.Contracts.Messaging;
using System.Text.Json;

namespace IngestionService.Messaging;

public sealed class ReadingsConsumer : IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ReadingRepository _repository;
    private readonly ILogger<ReadingsConsumer> _logger;

    private const string QueueName = "readings.ingestion";

    private ReadingsConsumer(
        IConnection connection,
        IChannel channel,
        ReadingRepository repository,
        ILogger<ReadingsConsumer> logger)
    {
        _connection = connection;
        _channel = channel;
        _repository = repository;
        _logger = logger;
    }

    public static async Task<ReadingsConsumer> CreateAsync(
        RabbitMqConnectionFactory factory,
        ReadingRepository repository,
        ILogger<ReadingsConsumer> logger)
    {
        var connection = await factory.CreateConnectionAsync("ingestion-consumer");
        var channel = await connection.CreateChannelAsync();

        // Fanout exchange — declared here too, idempotent
        await channel.ExchangeDeclareAsync(
            exchange: ExchangeNames.Readings,
            type: ExchangeType.Fanout,
            durable: true);

        // Durable queue — readings survive if ingestion goes offline
        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        await channel.QueueBindAsync(
            queue: QueueName,
            exchange: ExchangeNames.Readings,
            routingKey: string.Empty);  // fanout ignores routing key

        // One at a time — don't outpace the database
        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false);

        return new ReadingsConsumer(connection, channel, repository, logger);
    }

    public async Task StartConsumingAsync(CancellationToken ct = default)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnReadingReceivedAsync;

        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: ct);

        _logger.LogInformation(
            "Ingestion consumer listening on queue '{Queue}'", QueueName);
    }

    private async Task OnReadingReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        AssetReading? reading = null;
        try
        {
            reading = JsonSerializer.Deserialize<AssetReading>(ea.Body.Span);
            if (reading is null) throw new InvalidOperationException("Null reading payload.");

            await _repository.SaveAsync(reading, ea.CancellationToken);

            _logger.LogInformation(
                "Persisted reading from {AssetId}: primary={Primary:F2} at {Timestamp}",
                reading.AssetId, reading.PrimaryValue, reading.Timestamp);

            await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to persist reading from {AssetId} — requeueing",
                reading?.AssetId);

            await _channel.BasicNackAsync(
                deliveryTag: ea.DeliveryTag,
                multiple: false,
                requeue: true);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
    }
}
