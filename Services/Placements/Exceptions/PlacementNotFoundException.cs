using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Services.Placements.Exceptions
{
    /// <summary>
    /// Thrown when an operation requires a current placement for an entity but none exists on the board.
    /// </summary>
    /// <remarks>Signals that the entity has never been placed, or that no placement row exists for it within the
    /// scope being queried. The application's exception handler does not map this type to a specific status code,
    /// so it currently surfaces as a server error unless the caller catches it.</remarks>
    /// <param name="entityId">Identifier of the entity whose placement was expected; it is included in the
    /// exception message.</param>
    public class PlacementNotFoundException(EntityId entityId) 
        : Exception($"No current placement found for entity '{entityId.Id}'.");
}
