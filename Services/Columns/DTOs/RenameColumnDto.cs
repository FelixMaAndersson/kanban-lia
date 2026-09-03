using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Services.Columns.DTOs
{
    public record RenameColumnDto(ColumnId Id, string NewTitle);
}
