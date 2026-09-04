namespace kanban_lia.Endpoints.Placements.Requests
{
    public record GetPlacementByColumnRequest(Guid[] EntityIds, Guid ColumnId);
}

