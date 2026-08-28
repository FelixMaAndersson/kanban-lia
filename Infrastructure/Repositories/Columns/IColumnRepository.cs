using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Infrastructure.Repositories.Columns
{
    public interface IColumnRepository
    {
        Task<Column> CreateAsync(Column column);
        Task<IEnumerable<Column>> GetByBoardIdAsync(BoardId boardId);
        Task<Column?> GetByIdAsync(ColumnId id);
        Task<bool> RenameAsync(ColumnId id, string title);

        //Kanske ha en move method också??
        Task<bool> DeleteAsync(ColumnId id);
    }
}