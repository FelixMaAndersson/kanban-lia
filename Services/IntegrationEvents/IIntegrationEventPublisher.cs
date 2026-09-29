namespace kanban_lia.Services.IntegrationEvents;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(
        string eventType,
        string content,
        CancellationToken cancellationToken);

    //Task PublishPlacementCreatedAsync(
    //    Guid entityId,
    //    Guid columnId,
    //    Guid? causationEventId,
    //    CancellationToken cancellationToken);

    Task PublishColumnHasNoEdgeAsync(
        Guid columnId,
        Guid? causationEventId,
        CancellationToken cancellationToken);
}