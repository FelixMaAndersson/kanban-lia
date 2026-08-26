using kanban_lia.Models.Domain;

namespace kanban_lia.Infrastructure.Repositories
{
    public interface IColumnRepository
    {
        Task CreateAsync(Column column);
        Task<Column?> GetByIdAsync(ColumnId id);
        Task RenameAsync(ColumnId id, string title);
        Task DeleteAsync(ColumnId id);
    }
}
