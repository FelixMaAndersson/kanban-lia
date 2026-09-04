using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Placements;


namespace kanban_lia.Services.Placements.DTOs
{
    public record GetPlacementDto(IEnumerable<EntityId> EntityIds, BoardId BoardId);
}