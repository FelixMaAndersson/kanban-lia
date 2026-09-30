using kanban_lia.Services.IntegrationEvents;
using kanban_lia.Services.IntegrationEvents.Contracts;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace kanban_lia.Infrastructure.Messaging;

public sealed class RabbitMqIntegrationEventPublisher(
    IOptions<RabbitMqOptions> options)
        : IIntegrationEventPublisher, IAsyncDisposable
{
    // private const string ColumnHasNoEdge = "ColumnHasNoEdge";
    private const string PlacementCreated = "PlacementCreated";
    private const string PlacementBackend = "PlacementBackend";

    private readonly RabbitMqOptions _options = options.Value;
    private readonly SemaphoreSlim _channelLock = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync(
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
                MessageId = Guid.NewGuid().ToString(),
                Type = eventType
            };

            await channel.BasicPublishAsync(
                exchange: _options.ExchangeName,
                routingKey: _options.RoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);
        }
        finally
        {
            _channelLock.Release();
        }
    }

    //public async Task PublishPlacementCreatedAsync(
    //    Guid entityId,
    //    Guid columnId,
    //    Guid? causationEventId,
    //    CancellationToken cancellationToken)
    //{
    //    var integrationEvent =
    //        new IntegrationEvent<PlacementCreatedPayload>(
    //            EventId: Guid.NewGuid().ToString(),
    //            CausationEventId: causationEventId,
    //            EventType: PlacementCreated,
    //            Source: PlacementBackend,
    //            Payload: new PlacementCreatedPayload(
    //                EntityId: entityId,
    //                ColumnId: columnId));

    //    var body = JsonSerializer.SerializeToUtf8Bytes(
    //        integrationEvent);

    //    await _channelLock.WaitAsync(cancellationToken);

    //    try
    //    {
    //        await EnsureConnectedAsync();

    //        var channel = _channel
    //            ?? throw new InvalidOperationException(
    //                "RabbitMQ channel was not initialized.");

    //        var properties = new BasicProperties
    //        {
    //            ContentType = "application/json",
    //            DeliveryMode = DeliveryModes.Persistent,
    //            MessageId = integrationEvent.EventId,
    //            Type = integrationEvent.EventType
    //        };

    //        await channel.BasicPublishAsync(
    //            exchange: _options.ExchangeName,
    //            routingKey: _options.RoutingKey,
    //            mandatory: false,
    //            basicProperties: properties,
    //            body: body,
    //            cancellationToken: cancellationToken);
    //    }
    //    finally
    //    {
    //        _channelLock.Release();
    //    }
    //}

    //public async Task PublishColumnHasNoEdgeAsync(
    //    Guid columnId,
    //    Guid? causationEventId,
    //    CancellationToken cancellationToken)
    //{
    //    var integrationEvent =
    //    new IntegrationEvent<ColumnHasNoEdgePayload>(
    //        EventId: Guid.NewGuid().ToString(),
    //        CausationEventId: causationEventId,
    //        EventType: ColumnHasNoEdge,
    //        Source: PlacementBackend,
    //        Payload: new ColumnHasNoEdgePayload(
    //            ColumnId: columnId));

    //    var body = JsonSerializer.SerializeToUtf8Bytes(
    //    integrationEvent);

    //    await _channelLock.WaitAsync(cancellationToken);

    //    try
    //    {
    //        await EnsureConnectedAsync();

    //        var channel = _channel
    //            ?? throw new InvalidOperationException(
    //                "RabbitMQ channel was not initialized.");

    //        var properties = new BasicProperties
    //        {
    //            ContentType = "application/json",
    //            DeliveryMode = DeliveryModes.Persistent,
    //            MessageId = integrationEvent.EventId.ToString(),
    //            Type = integrationEvent.EventType
    //        };

    //        await channel.BasicPublishAsync(
    //            exchange: _options.ExchangeName,
    //            routingKey: "column.no-edge",
    //            mandatory: false,
    //            basicProperties: properties,
    //            body: body,
    //            cancellationToken: cancellationToken);
    //    }
    //    finally
    //    {
    //        _channelLock.Release();
    //    }
    //}

    private async Task EnsureConnectedAsync()
    {
        if (_connection?.IsOpen == true &&
            _channel?.IsOpen == true)
        {
            return;
        }

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            UserName = _options.UserName,
            Password = _options.Password,
            AutomaticRecoveryEnabled = true
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

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