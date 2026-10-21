

namespace kanban_lia.Infrastructure.Messaging
{
    /// <summary>
    /// Configuration options for connecting to RabbitMQ and publishing integration events.
    /// </summary>
    /// <remarks>Bound from the "RabbitMq" configuration section and consumed by the integration event publisher, which
    /// opens a connection with the given credentials, declares a durable topic exchange, and publishes persistent
    /// messages with the configured routing key.</remarks>
    public sealed class RabbitMqOptions
    {
        public string HostName { get; init; } = "localhost";

        public string UserName { get; init; } = string.Empty;

        public string Password { get; init; } = string.Empty;

        public string ExchangeName { get; init; } = "placement.events";

        public string RoutingKey { get; init; } = "placement.created";
    }
}