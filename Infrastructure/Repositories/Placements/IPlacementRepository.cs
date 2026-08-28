using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;
using kanban_lia.Services.Placements;

namespace kanban_lia.Infrastructure.Repositories.Placements
{
    public interface IPlacementRepository
    {
        Task CreateAsync(Placement placement);
        Task<Placement?> GetCurrentAsync(EntityId entityId, BoardId boardId);
        Task<string?> GetCurrentSortKeyAsync(EntityId entityId, ColumnId columnId);   
        
        Task<string?> GetNextSortKeyAsync(string sortKey, ColumnId columnId);

        Task<string?> GetFirstSortKeyAsync(ColumnId columnId);
    }
}
