namespace kanban_lia.Endpoints.Requests.Column
{
    public record CreateColumnRequest(string Title, int Position, Guid BoardId);
}
