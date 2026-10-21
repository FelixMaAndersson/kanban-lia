namespace kanban_lia.Infrastructure.Repositories.Outbox
{
    /// <summary>
    /// Represents one message in the transactional outbox awaiting publication or already published.
    /// </summary>
    /// <remarks>Messages are inserted in the same transaction as the domain change that produced them, so a commit
    /// either persists both or neither. A background publisher reads messages whose <c>ProcessedOn</c> is
    /// null, oldest first, and publishes them; failures are recorded in <c>Error</c> and counted in
    /// <c>RetryCount</c>.</remarks>
    /// <param name="Id">Unique identifier of the message, also used as the broker message ID.</param>
    /// <param name="EventType">The type of the event, e.g. "PlacementCreated".</param>
    /// <param name="Content">The serialized JSON payload of the event.</param>
    /// <param name="OccurredOn">When the message was written to the outbox; used to order publication.</param>
    /// <param name="ProcessedOn">When the message was successfully published; null while it is still pending.</param>
    /// <param name="Error">The last publication error, or null if publication has not failed.</param>
    /// <param name="RetryCount">Number of failed publication attempts; 0 until the first failure.</param>
    public sealed record OutboxMessage(
        Guid Id,
        string EventType,
        string Content,
        DateTime OccurredOn,
        DateTime? ProcessedOn,
        string? Error,
        int RetryCount = 0
    );
}
