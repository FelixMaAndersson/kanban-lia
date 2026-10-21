using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Models.Domain.Placements.DTOs
{
    /// <summary>
    /// Data-transfer shape for a placement, describing where an entity currently sits on a board.
    /// </summary>
    /// <remarks>Produced from the <see cref="Placement"/> domain model when placements are returned over the API.
    /// The Placements table keeps every historical move; the current placement of an entity is the row with the
    /// greatest <paramref name="Timestamp"/>, and <paramref name="SortKey"/> is a string that orders placements
    /// within a column under binary collation.</remarks>
    /// <param name="EntityId">The entity whose placement this describes.</param>
    /// <param name="BoardId">The board the placement belongs to.</param>
    /// <param name="ColumnId">The column the entity is placed in.</param>
    /// <param name="Timestamp">When this placement was recorded; the row with the latest timestamp is the current
    /// placement.</param>
    /// <param name="SortKey">The string sort key that orders the entity within its column.</param>
    public record PlacementDto(
        EntityId EntityId,
        BoardId BoardId,
        ColumnId ColumnId,
        DateTime Timestamp,
        string SortKey
    );
}
