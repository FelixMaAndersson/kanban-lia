using kanban_lia.Models.Domain.Boards;

namespace kanban_lia.Models.Domain.Columns.DTOs
{
    public record ColumnDto(
        ColumnId Id,
        BoardId BoardId,
        string Title,
        int Position,
        bool RequestWritable
    );
}
