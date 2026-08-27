namespace kanban_lia.Endpoints.Boards.Requests
{
    public record RenameBoardRequest(Guid Id, string NewTitle);
}
