using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Services.Placements.DTOs
{
    /// <summary>
    /// Query DTO selecting the current placements of a set of entities on a specific board.
    /// </summary>
    /// <remarks>Used by the placement service to look up where each entity is currently placed; only the latest
    /// placement per entity within the board is returned.</remarks>
    /// <param name="EntityIds">Identifiers of the entities whose current placements are requested.</param>
    /// <param name="BoardId">Identifier of the board to search for placements in.</param>
    public record GetPlacementDto(
        IEnumerable<EntityId> EntityIds,
        BoardId BoardId);
}
