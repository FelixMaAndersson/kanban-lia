using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Services.Columns.DTOs

{
    public record CreateColumnDto(ColumnId? Id, string Title, int Position, BoardId BoardId);
}
