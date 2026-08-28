using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Services.Columns.DTOs;

namespace kanban_lia.Services.Columns
{
    public interface IColumnService
    {
        Task CreateAsync(CreateColumnDto dto);
        Task<IEnumerable<Column>> GetByBoardIdAsync(BoardId boardId);
        Task<Column?> GetByIdAsync(ColumnId id);
        Task<bool> RenameAsync(RenameColumnDto dto);
        Task<bool> DeleteAsync(ColumnId id);
    }
}