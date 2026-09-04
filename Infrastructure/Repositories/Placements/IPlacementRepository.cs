using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Infrastructure.Repositories.Placements
{
    public record SortKeyRange(string? Previous, string? Next);
    public enum SortKeyLookup
    {
        Last = 0,
        After = 1,
        Before = 2
    }
    public interface IPlacementRepository
    {
        Task CreateAsync(Placement placement);
        Task<IEnumerable<Placement>> GetCurrentAsync(IEnumerable<EntityId> entityIds, BoardId boardId);
        Task<IEnumerable<Placement>> GetCurrentByBoardAsync(BoardId boardId);
        Task<IEnumerable<Placement>> GetCurrentByColumnAsync(IEnumerable<EntityId> entityIds, ColumnId columnId);
        Task<SortKeyRange> GetSortKeyRangeAsync(
            ColumnId columnId,
            SortKeyLookup lookup,
            EntityId? afterEntityId = null,
            EntityId? beforeEntityId = null);
    }
}