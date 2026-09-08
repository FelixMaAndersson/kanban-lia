namespace kanban_lia.Endpoints.Placements.Requests
{
    public record CreatePlacementRequest(Guid[] EntityIds, Guid BoardId, Guid ColumnId, Guid? AfterEntityId, Guid? BeforeEntityId, Guid? SourceColumnId);
}
