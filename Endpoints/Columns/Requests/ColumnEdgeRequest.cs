namespace kanban_lia.Endpoints.Columns.Requests
{
    public record ColumnEdgeRequest(Guid FromColumnId, Guid ToColumnId);
}
