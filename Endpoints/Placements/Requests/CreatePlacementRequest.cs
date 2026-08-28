namespace kanban_lia.Endpoints.Placements.Requests
{
    public record CreatePlacementRequest(Guid EntityId, Guid ColumnId, Guid? AfterEntityId);
}
