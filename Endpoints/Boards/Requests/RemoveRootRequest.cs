namespace kanban_lia.Endpoints.Boards.Requests
{
    public record RemoveRootRequest(Guid BoardId, Guid EntityId);
}
