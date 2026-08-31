using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Services.Placements.DTOs
{
    public record CreatePlacementDto(Guid EntityId, ColumnId ColumnId, Guid? AfterEntityId, Guid? BeforeEntityId);
}
