using kanban_lia.Models.Domain.Boards;

namespace kanban_lia.Endpoints.Placements.Requests
{
    public record GetPlacementRequest(Guid entityId, Guid boardId);
}
