using System.Text.Json.Serialization;

using kanban_lia.Services.IntegrationEvents.Models;

namespace kanban_lia.Services.IntegrationEvents.Contracts
{
    /// <summary>
    /// Generic contract for events published by kanban-lia to other systems, carrying a strongly typed payload.
    /// </summary>
    /// <remarks>Serialized with camel-case JSON property names so the wire format matches the automation engine's
    /// event contract. Every event gets a fresh <paramref name="EventId"/>; when an event is published as a reaction
    /// to another event, the triggering event's id is recorded in <paramref name="CausationEventId"/> and the
    /// <paramref name="CorrelationId"/> is carried over so a chain of causally linked events can be traced end to
    /// end.</remarks>
    /// <param name="EventId">The unique identifier of this event, generated anew for each event.</param>
    /// <param name="EventType">The type of event, e.g. "PlacementCreated".</param>
    /// <param name="Source">The name of the system the event originates from.</param>
    /// <param name="CompanyId">The company that the actor who triggered the event belongs to.</param>
    /// <param name="CorrelationId">Groups events belonging to the same causal chain; retained when one event triggers
    /// another.</param>
    /// <param name="CausationEventId">The id of the event that caused this one, when this event is a reaction to
    /// another event; otherwise <see langword="null"/>.</param>
    /// <param name="Actor">The person or automation that caused the event.</param>
    /// <param name="Payload">The event-specific data, typed by <typeparamref name="T"/>.</param>
    /// <typeparam name="T">The CLR type of the event payload.</typeparam>
    public sealed record IntegrationEvent<T>(

        [property: JsonPropertyName("eventId")]
        Guid EventId,

        [property: JsonPropertyName("eventType")]
        string EventType,

        [property: JsonPropertyName("source")]
        string Source,

        [property: JsonPropertyName("companyId")]
        int CompanyId,

        [property: JsonPropertyName("correlationId")]
        Guid CorrelationId,

        [property: JsonPropertyName("causationEventId")]
        Guid? CausationEventId,

        [property: JsonPropertyName("actor")]
        Actor Actor,

        [property: JsonPropertyName("payload")]
        T Payload);
}