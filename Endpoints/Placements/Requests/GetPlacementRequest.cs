namespace kanban_lia.Endpoints.Placements.Requests
{
    public record GetPlacementRequest(Guid[] EntityIds, Guid BoardId);
}
