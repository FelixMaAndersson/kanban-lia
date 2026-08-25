using kanban_lia.Domain;

namespace kanban_lia.Services
{
    public interface IBoardService
    {

        Task CreateAsync(Board board);
        Task<Board?> GetBoardByIdAsync(Guid id);
        Task UpdateAsync(Board board);
        Task DeleteAsync(Guid id);
    }
}
