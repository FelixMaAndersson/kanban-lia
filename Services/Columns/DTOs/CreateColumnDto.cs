using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Services.Columns.DTOs

{
    public record CreateColumnDto(ColumnId? Id, string Title, int Position, Guid BoardId);
}
