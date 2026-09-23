namespace kanban_lia.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public string HostName { get; init; } = "localhost";
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string ExchangeName { get; init; } = "placement.events";
    public string RoutingKey { get; init; } = "placement.created";
}