namespace kanban_lia.Endpoints.Boards.Requests
{
    /// <summary>
    /// Request body for removing an entity from a board's set of root entities.
    /// </summary>
    /// <param name="BoardId">The board to remove the root from.</param>
    /// <param name="EntityId">The entity that should no longer count as a root of the board.</param>
    public record RemoveRootRequest(
        Guid BoardId,
        Guid EntityId);
}
