namespace kanban_lia.Endpoints.Columns.Requests
{
    public record CreateColumnRequest(Guid BoardId, string Title, int Position, bool RequestWritable);
}
