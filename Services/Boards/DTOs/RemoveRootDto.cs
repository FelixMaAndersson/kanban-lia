using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Services.Boards.DTOs
{
    /// <summary>
    /// DTO carrying the identifiers needed to remove a root entity from a board.
    /// </summary>
    /// <remarks>Consumed by the board service, which verifies that the board and the root both exist before removing
    /// the entity from the board's root set.</remarks>
    /// <param name="BoardId">The board to remove the root from.</param>
    /// <param name="EntityId">The entity to remove from the board's roots.</param>
    public record RemoveRootDto(
        BoardId BoardId,
        EntityId EntityId);
}
