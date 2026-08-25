using kanban_lia.Domain;

namespace kanban_lia.Services
{
    public interface IColumnService
    {
        Task CreateAsync(Column column);
        Task<IEnumerable<Column>> GetAllColumnsAsync();
        Task<IEnumerable<Column>> GetColumnsByBoardIdAsync(Guid boardId);
        Task<Column?> GetColumnByIdAsync(Guid id);
        Task UpdateAsync(Column column);
        Task DeleteAsync(Guid id);
    }
}
