namespace kanban_lia.Endpoints.Requests
{
    public record RenameBoardRequest(Guid Id, string NewName);
}
