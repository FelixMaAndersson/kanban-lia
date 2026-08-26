using kanban_lia.Models.Domain;

namespace kanban_lia.Infrastructure.Repositories
{
    public interface IBoardRepository
    {
        Task CreateAsync(Board board);
        Task<Board?> GetByIdAsync(BoardId id);
        Task RenameAsync(BoardId id, string title);
        Task AddRoot(BoardId id, EntityId entityId);
        Task RemoveRoot(BoardId id, EntityId entityId);
        Task DeleteAsync(BoardId id);
    }
}
