using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements.DTOs;

namespace kanban_lia.Services.Placements.DTOs
{
    public record PlacementOperationDto(CreatePlacementDto Dto, ColumnId? SourceColumnId);
}
