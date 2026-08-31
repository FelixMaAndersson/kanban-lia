using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Boards;

namespace kanban_lia.Services.Columns.DTOs

{
    public record CreateColumnDto(ColumnId? Id, string Title, int Position, BoardId BoardId);
}
