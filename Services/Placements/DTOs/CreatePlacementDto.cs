using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Services.Placements.DTOs
{
    public record CreatePlacementDto(Guid EntityId, BoardId BoardId, ColumnId ColumnId, Guid? AfterEntityId, Guid? BeforeEntityId);
}
