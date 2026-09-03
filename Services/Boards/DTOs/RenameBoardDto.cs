using kanban_lia.Models.Domain.Boards;

namespace kanban_lia.Services.Boards.DTOs
{
    public record RenameBoardDto(BoardId Id, string NewTitle);
}
