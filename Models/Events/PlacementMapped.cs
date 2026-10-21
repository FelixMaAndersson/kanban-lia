using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Models.Events
{
    public record PlacementMapped(
        List<EntityId> EntityIds,
        Guid[] AfterEntityIds,
        Guid[] BeforeEntityIds,
        BoardId BoardId,
        List<ColumnId> ConnectedTargetColumnIds
    );
}
