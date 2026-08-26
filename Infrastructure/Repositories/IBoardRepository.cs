using kanban_lia.Models.Domain;

namespace kanban_lia.Infrastructure.Repositories
{
    public interface IBoardRepository
    {
        Task<BoardId> CreateAsync(Board board);
        Task<Board?> GetByIdAsync(BoardId id);
        Task<bool> RenameAsync(BoardId id, string title);
        Task<bool> AddRootAsync(BoardId id, EntityId entityId);
        Task<bool> RemoveRootAsync(BoardId id, EntityId entityId);
        Task<bool> DeleteAsync(BoardId id);
    }
}
