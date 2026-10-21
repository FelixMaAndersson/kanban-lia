namespace kanban_lia.Endpoints.Columns.Requests
{
    /// <summary>
    /// Record <c>ColumnEdgeRequest</c> represents a request to create a directed edge between two columns.
    /// </summary>
    /// <remarks>The edge expresses that the two columns are connected, which the placement endpoints use to resolve
    /// source and target columns across boards.</remarks>
    /// <param name="FromColumnId">Identifier of the column the edge starts from.</param>
    /// <param name="ToColumnId">Identifier of the column the edge points to.</param>
    public record ColumnEdgeRequest(
        Guid FromColumnId,
        Guid ToColumnId);
}
