using kanban_lia.Models.Domain.Boards;

namespace kanban_lia.Models.Domain.Columns.DTOs
{
    public record ColumnDto(
        ColumnId Id,
        string Title,
        int Position,
        BoardId BoardId
    );
}
