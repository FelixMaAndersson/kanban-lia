using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Services.Placements.DTOs
{
    /// <summary>
    /// Data-transfer request describing a batch of placements to create in a single column.
    /// </summary>
    /// <remarks>One DTO is produced per target column when entities move across boards, so a single user
    /// action that fans out to several boards results in several of these requests. The after/before anchors
    /// determine where in the column's sort order the entities are inserted.</remarks>
    /// <param name="EntityIds">The entities to place in the target column.</param>
    /// <param name="BoardId">The board that owns the target column; placements are scoped to this board.</param>
    /// <param name="ColumnId">The column the entities are placed in.</param>
    /// <param name="AfterEntityIds">Optional anchor entities that the new placements should follow in sort
    /// order; when empty and no before anchors are given, placements are appended.</param>
    /// <param name="BeforeEntityIds">Optional anchor entities that the new placements should precede in sort
    /// order.</param>
    public record CreatePlacementDto(
        IEnumerable<EntityId> EntityIds, 
        BoardId BoardId, 
        ColumnId ColumnId, 
        IEnumerable<EntityId> AfterEntityIds, 
        IEnumerable<EntityId> BeforeEntityIds);
}