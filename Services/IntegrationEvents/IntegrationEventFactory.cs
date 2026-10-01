using kanban_lia.Services.IntegrationEvents.Contracts;
using kanban_lia.Services.IntegrationEvents.Models;

namespace kanban_lia.Services.IntegrationEvents
{
    public static class IntegrationEventFactory
    {
        public static IntegrationEvent<PlacementCreatedPayload>
            CreatePlacementCreated(
                Guid entityId,
                Guid columnId,
                Guid correlationId,
                Guid? causationEventId,
                Actor actor)
        {
            return new IntegrationEvent<PlacementCreatedPayload>(
                EventId: Guid.NewGuid(),
                EventType: "PlacementCreated",
                Source: "PlacementBackend",
                CompanyId: Guid.Empty,
                CorrelationId: correlationId,
                CausationEventId: causationEventId,
                Actor: actor,
                Payload: new PlacementCreatedPayload(
                    EntityId: entityId,
                    ColumnId: columnId));
        }
    }
}
