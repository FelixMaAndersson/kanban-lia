using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Services.Placements.DTOs
{
    public record CreatePlacementDto(EntityId EntityId, BoardId BoardId, ColumnId ColumnId, Guid? AfterEntityId, Guid? BeforeEntityId);
}