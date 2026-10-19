using kanban_lia.Services.IntegrationEvents;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;

namespace kanban_lia.Infrastructure.Messaging;

public sealed class RabbitMqIntegrationEventPublisher(
    IOptions<RabbitMqOptions> options)
        : IIntegrationEventPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options = options.Value;
    private readonly SemaphoreSlim _channelLock = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync(
        Guid messageId,
        string eventType,
        string content,
        CancellationToken cancellationToken)
    {
        var body = Encoding.UTF8.GetBytes(content);

        await _channelLock.WaitAsync(cancellationToken);

        try
        {
            await EnsureConnectedAsync();

            var channel = _channel
                ?? throw new InvalidOperationException(
                    "RabbitMQ channel was not initialized.");

            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = messageId.ToString(),
                Type = eventType
            };

            await channel.BasicPublishAsync(
                exchange: _options.ExchangeName,
                routingKey: _options.RoutingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);
        }
        finally
        {
            _channelLock.Release();
        }
    }

    private async Task EnsureConnectedAsync()
    {
        if (_connection?.IsOpen == true &&
            _channel?.IsOpen == true)
        {
            return;
        }

        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            UserName = _options.UserName,
            Password = _options.Password,
            AutomaticRecoveryEnabled = true
        };

        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);


        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync(channelOptions);

        await _channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _channelLock.Dispose();
    }
}
