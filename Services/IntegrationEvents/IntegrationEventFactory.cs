using kanban_lia.Services.IntegrationEvents.Contracts;
using kanban_lia.Services.IntegrationEvents.Models;

namespace kanban_lia.Services.IntegrationEvents
{
    /// <summary>
    /// Creates integration events with the standard envelope metadata filled in.
    /// </summary>
    /// <remarks>Each created event gets a fresh event identifier and is stamped with the fixed event type and source
    /// "Kanban"; the caller supplies the causation and correlation identifiers so events form a traceable chain.
    /// </remarks>
    public static class IntegrationEventFactory
    {
        /// <summary>
        /// Creates a PlacementCreated integration event for a single placed entity.
        /// </summary>
        /// <remarks>The event is given a new event identifier, the event type "PlacementCreated", the source
        /// "Kanban", and a company identifier of 0. The resulting event is typically serialized into the outbox so
        /// it can be published to other systems.</remarks>
        /// <param name="entityId">Identifier of the entity that was placed.</param>
        /// <param name="columnId">Identifier of the column the entity was placed in.</param>
        /// <param name="correlationId">Identifier tying the event back to the originating command or request.</param>
        /// <param name="causationEventId">Identifier of the event that caused this one, if any.</param>
        /// <param name="actor">The actor that caused the placement.</param>
        /// <returns>The fully constructed PlacementCreated integration event.</returns>
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
                Source: "Kanban",
                CompanyId: 0,
                CorrelationId: correlationId,
                CausationEventId: causationEventId,
                Actor: actor,
                Payload: new PlacementCreatedPayload(
                    EntityId: entityId,
                    ColumnId: columnId));
        }
    }
}
