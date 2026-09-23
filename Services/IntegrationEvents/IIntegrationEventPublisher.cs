namespace kanban_lia.Services.IntegrationEvents;

public interface IIntegrationEventPublisher
{
    Task PublishPlacementCreatedAsync(
        Guid entityId,
        Guid columnId,
        CancellationToken cancellationToken);
}