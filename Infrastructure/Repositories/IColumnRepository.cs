using kanban_lia.Models.Domain;

namespace kanban_lia.Infrastructure.Repositories
{
    public interface IColumnRepository
    {
        Task CreateAsync(Column column);
        Task<IEnumerable<Column>> GetByBoardIdAsync(BoardId boardId);
        Task<Column?> GetByIdAsync(ColumnId id);
        Task<bool> RenameAsync(ColumnId id, string title);
        
        //Kanske ha en move method också??
        Task<bool> DeleteAsync(ColumnId id);
    }
}
