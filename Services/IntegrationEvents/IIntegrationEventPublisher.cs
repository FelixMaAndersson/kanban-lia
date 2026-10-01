using kanban_lia.Services.IntegrationEvents.Models;

namespace kanban_lia.Services.IntegrationEvents;

public interface IIntegrationEventPublisher
{
    Task PublishPlacementCreatedAsync(
        Guid entityId,
        Guid columnId,
        Guid? sourceColumnId,
        Guid? causationEventId,
        Actor actor,
        CancellationToken cancellationToken);

    //Task PublishColumnHasNoEdgeAsync(
    //    Guid columnId,
    //    Guid? causationEventId,
    //    Actor actor,
    //    CancellationToken cancellationToken);
}