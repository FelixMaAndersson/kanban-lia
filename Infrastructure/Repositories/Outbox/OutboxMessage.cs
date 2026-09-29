namespace kanban_lia.Infrastructure.Repositories.Outbox
{
    public sealed record OutboxMessage(
        Guid Id,
        string EventType,
        string Payload,
        DateTime OccurredOn,
        DateTime? ProcessedOn,
        string? Error
    );
}
