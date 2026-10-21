namespace kanban_lia.Endpoints.Placements.Requests
{
    /// <summary>
    /// Request body for placing one or more entities into a target column.
    /// </summary>
    /// <remarks>The after and before anchors are mutually exclusive; supplying both causes the request to fail with
    /// an <see cref="kanban_lia.Models.Domain.Exceptions.InvalidDomainException"/>. When neither is supplied the
    /// entities are appended at the end of the column.</remarks>
    /// <param name="EntityIds">The entities to place.</param>
    /// <param name="BoardId">The board the request originates from, used to resolve the entities' current placements.</param>
    /// <param name="ColumnId">The target column to place the entities in.</param>
    /// <param name="AfterEntityIds">Entities the placed entities should follow within the column; mutually exclusive with
    /// <paramref name="BeforeEntityIds"/>.</param>
    /// <param name="BeforeEntityIds">Entities the placed entities should precede within the column; mutually exclusive with
    /// <paramref name="AfterEntityIds"/>.</param>
    /// <param name="SourceColumnId">The column the entities are moved from, when the client specifies one.</param>
    public record CreatePlacementRequest(
        Guid[] EntityIds,
        Guid BoardId,
        Guid ColumnId,
        Guid[] AfterEntityIds,
        Guid[] BeforeEntityIds,
        Guid? SourceColumnId);
}
