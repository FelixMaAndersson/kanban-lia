using System.Text.Json.Serialization;

namespace kanban_lia.Services.IntegrationEvents.Contracts;

public sealed record ColumnHasNoEdgePayload(
    [property: JsonPropertyName("columnId")]
    Guid ColumnId
);

