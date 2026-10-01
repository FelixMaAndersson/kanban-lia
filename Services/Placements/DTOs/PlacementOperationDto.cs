using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Services.Placements.DTOs
{
    public record PlacementOperationDto(
        CreatePlacementDto Dto,
        IEnumerable<ColumnId> SourceColumnIds);
}
