using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Infrastructure.Repositories.Boards
{
    public interface IBoardRepository
    {
        Task<BoardId> CreateAsync(Board board);
        Task<Board?> GetByIdAsync(BoardId id);
        Task<bool> RenameAsync(BoardId id, string title);
        Task<bool> AddRootAsync(BoardId id, EntityId entityId);
        Task<bool> RemoveRootAsync(BoardId id, EntityId entityId);
        Task<bool> DeleteAsync(BoardId id);
        Task<bool> BoardExistsAsync(BoardId boardId);
        Task<bool> RootExistsAsync(BoardId boardId, EntityId entityId);
    }
}
