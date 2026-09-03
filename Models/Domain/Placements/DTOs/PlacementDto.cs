using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Models.Domain.Placements.DTOs
{
    public record PlacementDto(
        EntityId EntityId,
        BoardId BoardId,
        ColumnId ColumnId,
        DateTime TimeStamp,
        string SortKey
    );
}
