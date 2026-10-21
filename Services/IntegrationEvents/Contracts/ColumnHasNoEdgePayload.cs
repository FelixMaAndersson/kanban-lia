#if false
using System.Text.Json.Serialization;

namespace kanban_lia.Services.IntegrationEvents.Contracts
{
    /// <summary>
    /// Integration event contract for a column that has no edges, i.e. no connections to other columns.
    /// </summary>
    /// <remarks>Serialized with camelCase property names; currently excluded from compilation by the
    /// <c>#if false</c> directive wrapping this file.</remarks>
    /// <param name="ColumnId">The ID of the column that has no edges.</param>
    public sealed record ColumnHasNoEdgePayload(
        [property: JsonPropertyName("columnId")]
        Guid ColumnId
    );
}
#endif
