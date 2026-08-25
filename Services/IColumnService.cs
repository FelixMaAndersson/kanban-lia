using kanban_lia.Domain;

namespace kanban_lia.Services
{
    public interface IColumnService
    {
        Task CreateAsync(Column column);
        Task<IEnumerable<Column>> GetAllAsync();
        Task<IEnumerable<Column>> GetByBoardIdAsync(Guid boardId);
        Task<Column?> GetByIdAsync(Guid id);
        Task UpdateAsync(Column column);
        Task DeleteAsync(Guid id);
    }
}
