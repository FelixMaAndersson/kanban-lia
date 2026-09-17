using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Infrastructure.Repositories.Columns
{
    public interface IColumnEdgeRepository
    {
        Task CreateAsync(ColumnEdge columnEdge);
        Task<IEnumerable<ColumnEdge>> GetByFromColumnIdAsync(ColumnId fromColumnId);
        Task<IEnumerable<ColumnEdge>> GetByToColumnIdAsync(ColumnId toColumnId);
        Task<bool> DeleteAsync(ColumnEdge columnEdge);
    }
}
