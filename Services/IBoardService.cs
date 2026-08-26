using kanban_lia.Domain;

namespace kanban_lia.Services
{
    public interface IBoardService
    {
        Task CreateAsync(Board board);
        Task<Board?> GetByIdAsync(BoardId id);
        Task UpdateAsync(Board board);
        Task DeleteAsync(BoardId id);
        Task<Board> RenameAsync(BoardId id, string title);
        Task<Board> AddRootAsync(BoardId id, Guid entityId);
    }
}
