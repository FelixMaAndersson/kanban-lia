using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Models.Domain.Boards.DTOs
{
    public record BoardDto(
        BoardId Id,
        string Title,
        IReadOnlyCollection<EntityId> Roots
    );
}