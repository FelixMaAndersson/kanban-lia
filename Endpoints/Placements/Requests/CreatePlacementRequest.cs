namespace kanban_lia.Endpoints.Placements.Requests
{
    public record CreatePlacementRequest(Guid EntityId, Guid BoardId, Guid ColumnId, Guid? AfterEntityId, Guid? BeforeEntityId);
}
