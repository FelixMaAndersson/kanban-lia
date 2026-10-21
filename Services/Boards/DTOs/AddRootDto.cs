using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Services.Boards.DTOs
{
    /// <summary>
    /// Service-level DTO carrying the instruction to register an entity as a root of a board.
    /// </summary>
    /// <remarks>Produced by mapping an <c>AddRootRequest</c> from the boards endpoints; the identifiers are strong
    /// domain types rather than raw Guids.</remarks>
    /// <param name="BoardId">Identifier of the board the entity should become a root of.</param>
    /// <param name="EntityId">Identifier of the entity to register as a board root.</param>
    public record AddRootDto(
        BoardId BoardId,
        EntityId EntityId);
}
