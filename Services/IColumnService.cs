using kanban_lia.Domain;

namespace kanban_lia.Services
{
    public interface IColumnService
    {
        Task CreateAsync(Column column);
        Task<IEnumerable<Column>> GetAllAsync();
        Task<IEnumerable<Column>> GetByBoardIdAsync(BoardId boardId);
        Task<Column?> GetByIdAsync(ColumnId id);
        Task UpdateAsync(Column column);
        Task DeleteAsync(ColumnId id);
    }
}
