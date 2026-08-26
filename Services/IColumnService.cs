using kanban_lia.Models.Domain;
using kanban_lia.Services.DTOs;

namespace kanban_lia.Services
{
    public interface IColumnService
    {
        Task CreateAsync(CreateColumnDto dto);
        Task<IEnumerable<Column>> GetByBoardIdAsync(BoardId boardId);
        Task<Column?> GetByIdAsync(ColumnId id);
        Task RenameAsync(RenameColumnDto dto);
        Task DeleteAsync(ColumnId id);
    }
}
