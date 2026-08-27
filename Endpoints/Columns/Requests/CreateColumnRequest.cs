namespace kanban_lia.Endpoints.Columns.Requests
{
    public record CreateColumnRequest(string Title, int Position, Guid BoardId);
}
