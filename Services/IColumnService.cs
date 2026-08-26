using kanban_lia.Models.Domain;
using kanban_lia.Services.DTOs;

namespace kanban_lia.Services
{
    public interface IColumnService
    {
        Task<Column> CreateAsync(CreateColumnDto dto);
        Task<IEnumerable<Column>> GetByBoardIdAsync(BoardId boardId);
        Task<Column?> GetByIdAsync(ColumnId id);
        Task<bool> RenameAsync(RenameColumnDto dto);
        Task<bool> DeleteAsync(ColumnId id);
    }
}
