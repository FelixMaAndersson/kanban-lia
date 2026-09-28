namespace kanban_lia.Services.IntegrationEvents;

public interface IIntegrationEventPublisher
{
    Task PublishPlacementCreatedAsync(
        Guid entityId,
        Guid columnId,
        Guid? sourceColumnId,
        Guid? causationEventId,
        CancellationToken cancellationToken);

    Task PublishColumnHasNoEdgeAsync(
        Guid columnId,
        Guid? causationEventId,
        CancellationToken cancellationToken);
}