namespace kanban_lia.Endpoints.Placements.Requests
{
    public record CreatePlacementRequest(Guid EntityId, Guid ColumnId, string Position);
}
