using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Services.Placements.DTOs
{
    /// <summary>
    /// Pairs a single create-placement request with the source columns the entities are moved from on that request's
    /// target board.
    /// </summary>
    /// <remarks>A placement request can target columns on several boards connected to the destination; the endpoint
    /// builds one operation per target board so each <see cref="CreatePlacementDto"/> describes a single
    /// transaction-scoped move. <paramref name="SourceColumnIds"/> records where the entities come from and is used to
    /// build the from/to changes reported back to clients after the move.</remarks>
    /// <param name="Dto">The placement to create, naming the target board and column together with the entities and
    /// their optional before/after anchors.</param>
    /// <param name="SourceColumnIds">The columns on the target board the entities are considered to move from; empty
    /// when the entities are placed rather than moved.</param>
    public record PlacementOperationDto(
        CreatePlacementDto Dto,
        IEnumerable<ColumnId> SourceColumnIds);
}
