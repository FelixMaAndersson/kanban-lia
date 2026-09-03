using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Services.Boards.DTOs
{
    public record RemoveRootDto(BoardId BoardId, EntityId EntityId);
}
