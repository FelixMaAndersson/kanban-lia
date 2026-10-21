using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Services.Placements.DTOs
{
    /// <summary>
    /// Describes why placing a single entity failed.
    /// </summary>
    /// <remarks>Entities are processed independently, so one entity's failure does not stop others; the error is the
    /// message of the placement exception that was caught for that entity.</remarks>
    /// <param name="EntityId">The entity whose placement failed.</param>
    /// <param name="Error">The reason the placement failed.</param>
    public sealed record PlacementFailure(EntityId EntityId, string Error);

    /// <summary>
    /// Result of a placement operation, listing the entities placed and those that failed.
    /// </summary>
    /// <remarks>The two lists are disjoint: every requested entity appears in exactly one of them. Callers use the
    /// failures to decide whether to reject the request or report partial success.</remarks>
    /// <param name="SucceededEntityIds">The entities that were placed successfully.</param>
    /// <param name="Failures">The entities that could not be placed, with the reason for each.</param>
    public sealed record PlacementCreateResult(
        IReadOnlyList<EntityId> SucceededEntityIds,
        IReadOnlyList<PlacementFailure> Failures);
}
