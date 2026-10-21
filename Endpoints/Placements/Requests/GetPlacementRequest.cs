namespace kanban_lia.Endpoints.Placements.Requests
{
    /// <summary>
    /// Request payload for retrieving the current placements of a set of entities on a board.
    /// </summary>
    /// <remarks>Bound from the query string of the GET /api/placements/get endpoint (repeated <c>entityIds</c>
    /// parameters plus <c>boardId</c>) and mapped to a
    /// <see cref="kanban_lia.Services.Placements.DTOs.GetPlacementDto"/> before being passed to the placement
    /// service. Only the placement with the latest timestamp per entity is returned.</remarks>
    /// <param name="EntityIds">The entities whose placements should be looked up.</param>
    /// <param name="BoardId">The board whose placements are searched; entities placed on other boards are not
    /// returned.</param>
    public record GetPlacementRequest(
        Guid[] EntityIds,
        Guid BoardId);
}
