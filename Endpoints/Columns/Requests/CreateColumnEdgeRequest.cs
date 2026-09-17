namespace kanban_lia.Endpoints.Columns.Requests
{
    public record CreateColumnEdgeRequest(Guid FromColumnId, Guid ToColumnId);
}
