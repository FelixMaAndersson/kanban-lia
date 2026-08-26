using kanban_lia.Models.Domain;

namespace kanban_lia.Infrastructure.Repositories
{
    public interface IColumnRepository
    {
        Task<Column> CreateAsync(Column column);
        Task<IEnumerable<Column>> GetByBoardIdAsync(BoardId boardId);
        Task<Column?> GetByIdAsync(ColumnId id);
        Task RenameAsync(ColumnId id, string title);
        Task DeleteAsync(ColumnId id);
    }
}
