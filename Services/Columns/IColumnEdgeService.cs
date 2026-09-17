using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Columns.DTOs;
using kanban_lia.Services.Columns.DTOs;

namespace kanban_lia.Services.Columns
{
    public interface IColumnEdgeService
    {
        Task CreateAsync(CreateColumnEdgeDto dto);
        Task<IEnumerable<ColumnEdgeDto>> GetByBoardIdAsync(BoardId boardId);
        Task<IEnumerable<ColumnEdgeDto>> GetByFromColumnIdAsync(ColumnId fromColumnId);
        Task<IEnumerable<ColumnEdgeDto>> GetByToColumnIdAsync(ColumnId toColumnId);
        Task<bool> DeleteAsync(ColumnEdge edge);
    }
}
