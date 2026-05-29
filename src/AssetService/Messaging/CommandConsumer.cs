using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Messages;
using Shared.Contracts.Messaging;
using System.Text.Json;

namespace AssetService.Messaging;

public sealed class CommandConsumer : IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<CommandConsumer> _logger;
    private readonly string _assetId;
    private readonly string _queueName;

    private CommandConsumer(
        IConnection connection,
        IChannel channel,
        string assetId,
        string queueName,
        ILogger<CommandConsumer> logger)
    {
        _connection = connection;
        _channel = channel;
        _assetId = assetId;
        _queueName = queueName;
        _logger = logger;
    }

    public static async Task<CommandConsumer> CreateAsync(
        RabbitMqConnectionFactory factory,
        IOptions<AssetOptions> options,
        ILogger<CommandConsumer> logger)
    {
        var assetId = options.Value.Id;
        var queueName = $"commands.{assetId}";

        var connection = await factory.CreateConnectionAsync($"asset-consumer-{assetId}");
        var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: ExchangeNames.Commands,
            type: ExchangeType.Direct,
            durable: true);

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        await channel.QueueBindAsync(
            queue: queueName,
            exchange: ExchangeNames.Commands,
            routingKey: assetId);

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false);

        return new CommandConsumer(connection, channel, assetId, queueName, logger);
    }

    public async Task StartConsumingAsync(CancellationToken ct = default)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnCommandReceivedAsync;

        await _channel.BasicConsumeAsync(
            queue: _queueName,
            autoAck: false,
            consumerTag: _queueName,
            consumer: consumer,
            cancellationToken: ct);

        _logger.LogInformation(
            "Listening for commands on queue '{Queue}'", _queueName);
    }

    private async Task OnCommandReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        AssetCommand? command = null;
        try
        {
            command = JsonSerializer.Deserialize<AssetCommand>(ea.Body.Span);
            if (command is null) throw new InvalidOperationException("Null command payload.");

            await ExecuteCommandAsync(command);

            await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[{AssetId}]Failed to process command {CommandType} — requeueing",
                _assetId, command?.Type);

            await _channel.BasicNackAsync(
                deliveryTag: ea.DeliveryTag,
                multiple: false,
                requeue: true);
        }
    }

    private Task ExecuteCommandAsync(AssetCommand command)
    {
        switch (command.Type)
        {
            case CommandType.RunDiagnostic:
                _logger.LogInformation(
                    "[{AssetId}] Executing RunDiagnostic (issued at {IssuedAt})",
                    command.AssetId, command.IssuedAt);
                break;

            case CommandType.Calibrate:
                _logger.LogInformation(
                    "[{AssetId}] Executing Calibrate (issued at {IssuedAt})",
                    command.AssetId, command.IssuedAt);
                break;

            case CommandType.ChangeReportingFrequency:
                var freq = command.Parameters?.GetValueOrDefault("intervalSecs") ?? "?";
                _logger.LogInformation(
                    "[{AssetId}] Changing reporting frequency to {Freq}s (issued at {IssuedAt})",
                    command.AssetId, freq, command.IssuedAt);
                break;

            default:
                _logger.LogWarning(
                    "[{AssetId}] Unknown command type: {Type}", command.AssetId, command.Type);
                break;
        }

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
    }
}
