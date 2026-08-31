using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;
using kanban_lia.Services.Placements;

namespace kanban_lia.Infrastructure.Repositories.Placements
{
    public record SortKeyRange(string? Previous, string? Next);
    public enum SortKeyLookup
    {
        Empty = 0,
        Before = 1,
        After = 2
    }
    public interface IPlacementRepository
    {
        Task CreateAsync(Placement placement);
        Task<Placement?> GetCurrentAsync(EntityId entityId, BoardId boardId);
        Task<SortKeyRange> GetSortKeyRangeAsync(
            ColumnId columnId,
            SortKeyLookup lookup,
            EntityId? afterEntityId = null,
            EntityId? beforeEntityId = null);

    }
}