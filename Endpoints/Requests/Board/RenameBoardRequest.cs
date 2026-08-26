namespace kanban_lia.Endpoints.Requests.Board
{
    public record RenameBoardRequest(Guid Id, string NewTitle);
}
