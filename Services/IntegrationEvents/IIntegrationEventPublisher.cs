namespace kanban_lia.Services.IntegrationEvents;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(
        Guid messageId,
        string eventType,
        string content,
        CancellationToken cancellationToken);

    //Task PublishPlacementCreatedAsync(
    //    Guid entityId,
    //    Guid columnId,
    //    Guid? causationEventId,
    //    Actor actor,
    //    CancellationToken cancellationToken);

    //Task PublishColumnHasNoEdgeAsync(
    //    Guid columnId,
    //    Guid? causationEventId,
    //    Actor actor,
    //    CancellationToken cancellationToken);
}
