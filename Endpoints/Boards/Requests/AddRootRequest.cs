namespace kanban_lia.Endpoints.Boards.Requests
{
    /// <summary>
    /// Request body for registering an entity as a root of a board.
    /// </summary>
    /// <remarks>Mapped by AutoMapper to <c>AddRootDto</c> before the boards endpoint passes it to the board
    /// service.</remarks>
    /// <param name="BoardId">Identifier of the board the entity should become a root of.</param>
    /// <param name="EntityId">Identifier of the entity to register as a board root.</param>
    public record AddRootRequest(
        Guid BoardId,
        Guid EntityId);
}
