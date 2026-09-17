using kanban_lia.Models.Domain.Columns;
using kanban_lia.Services.Columns.DTOs;

namespace kanban_lia.Services.Columns
{
    public interface IColumnEdgeService
    {
        Task CreateAsync(CreateColumnEdgeDto dto);
        Task<IEnumerable<ColumnEdge>> GetByFromColumnIdAsync(ColumnId fromColumnId);
        Task<IEnumerable<ColumnEdge>> GetByToColumnIdAsync(ColumnId toColumnId);
        Task<bool> DeleteAsync(ColumnEdge edge);
    }
}
