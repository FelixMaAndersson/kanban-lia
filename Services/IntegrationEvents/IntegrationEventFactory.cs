using kanban_lia.Services.IntegrationEvents.Contracts;

namespace kanban_lia.Services.IntegrationEvents
{
    public static class IntegrationEventFactory
    {
        public static IntegrationEvent<PlacementCreatedPayload>
            CreatePlacementCreated(
                Guid entityId,
                Guid columnId,
                Guid? causationEventId)
        {
            return new IntegrationEvent<PlacementCreatedPayload>(
                EventId: Guid.NewGuid().ToString(),
                CausationEventId: causationEventId,
                EventType: "PlacementCreated",
                Source: "PlacementBackend",
                Payload: new PlacementCreatedPayload(
                    EntityId: entityId,
                    ColumnId: columnId));
        }
    }
}
