using System.Text.Json.Serialization;

namespace kanban_lia.Services.IntegrationEvents.Contracts;

public sealed record IntegrationEvent<TPayload>(
    [property: JsonPropertyName("eventId")]
    string EventId,

    [property: JsonPropertyName("eventType")]
    string EventType,

    [property: JsonPropertyName("source")]
    string Source,

    [property: JsonPropertyName("payload")]
    TPayload Payload);