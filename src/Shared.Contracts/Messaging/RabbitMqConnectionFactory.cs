using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Shared.Contracts.Messaging;

public sealed class RabbitMqConnectionFactory(IOptions<RabbitMqOptions> options)
{
    private readonly RabbitMqOptions _options = options.Value;

    public async Task<IConnection> CreateConnectionAsync(string clientProvidedName)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.Username,
            Password = _options.Password,
            VirtualHost = _options.VHost,
            ClientProvidedName = clientProvidedName
        };

        return await factory.CreateConnectionAsync();
    }
}
