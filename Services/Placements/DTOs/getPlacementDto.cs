using kanban_lia.Models.Domain.Boards;


namespace kanban_lia.Services.Placements.DTOs
{
    public record GetPlacementDto(Guid entityId, BoardId boardId);
}