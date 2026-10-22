using RabbitMQ.Client;

using Microsoft.Extensions.Options;

using System.Text;

using kanban_lia.Services.IntegrationEvents;

namespace kanban_lia.Infrastructure.Messaging
{
    /// <summary>
    /// Publishes integration events as JSON messages to a RabbitMQ topic exchange.
    /// </summary>
    /// <remarks>The publisher connects lazily on first use and reuses a single channel for all subsequent
    /// publishes. Concurrent publishes are serialized through a semaphore, and the channel is created with publisher
    /// confirmations enabled so a publish only completes once the broker has acknowledged it. Messages are marked
    /// persistent and published with the mandatory flag. Dispose the publisher (it implements
    /// <see cref="IAsyncDisposable"/>) to release the channel and connection.</remarks>
    /// <param name="options">RabbitMQ connection and topology settings bound from configuration.</param>
    public sealed class RabbitMqIntegrationEventPublisher(
        IOptions<RabbitMqOptions> options)
            : IIntegrationEventPublisher, IAsyncDisposable
    {
        private readonly RabbitMqOptions _options = options.Value;
        private readonly SemaphoreSlim _channelLock = new(1, 1);
        private IConnection? _connection;
        private IChannel? _channel;

        /// <summary>
        /// Publishes a single integration event message to the configured exchange and routing key.
        /// </summary>
        /// <remarks>Establishes the connection and channel on first use, reusing them afterwards; the channel lock
        /// ensures only one publish is in flight at a time. The message is delivered persistent with JSON content
        /// type, <paramref name="messageId"/> as the broker message id, and <paramref name="eventType"/> as the
        /// message type property.</remarks>
        /// <param name="messageId">Identifier of the message, used as the broker message id and for downstream
        /// deduplication.</param>
        /// <param name="eventType">The event type discriminator, published as the message type property.</param>
        /// <param name="content">The serialized (JSON) event payload used as the message body.</param>
        /// <param name="cancellationToken">A token used to cancel the publish while it is in flight.</param>
        /// <returns>A task that completes once the broker has confirmed the message.</returns>
        /// <exception cref="InvalidOperationException">The underlying channel is not initialized.</exception>
        /// <exception cref="OperationCanceledException">The operation is canceled through
        /// <paramref name="cancellationToken"/>.</exception>
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

        /// <summary>
        /// Reconnects and recreates the channel, declaring the topic exchange, when either is missing or closed;
        /// otherwise returns immediately.
        /// </summary>
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

            const string consumerQueue = "automation.placement.events";

            await _channel.QueueDeclareAsync(
                queue: consumerQueue,
                durable: true,
                exclusive: false,
                autoDelete: false);

            await _channel.QueueBindAsync(
                queue: consumerQueue,
                exchange: _options.ExchangeName,
                routingKey: "#");
        }

        /// <summary>
        /// Disposes the RabbitMQ channel and connection and releases the publish lock.
        /// </summary>
        /// <returns>A value task that completes when all resources have been released.</returns>
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
}
