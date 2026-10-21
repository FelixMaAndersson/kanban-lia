using System.Text.Json.Serialization;

namespace kanban_lia.Services.IntegrationEvents.Contracts
{
    /// <summary>
    /// Payload of the "PlacementCreated" integration event published to downstream systems.
    /// </summary>
    /// <remarks>Serialized with the explicit camel-case property names declared on the parameters, independent of
    /// the serializer's naming policy, so automation conditions can address the fields as "payload.entityId" and
    /// "payload.columnId". The event carries only this payload; correlation, causation and actor travel in the
    /// surrounding event envelope.</remarks>
    /// <param name="EntityId">Identifier of the entity that was placed.</param>
    /// <param name="ColumnId">Identifier of the column the entity was placed in.</param>
    public sealed record PlacementCreatedPayload(
        [property: JsonPropertyName("entityId")]
        Guid EntityId,

        [property: JsonPropertyName("columnId")]
        Guid ColumnId);
}
