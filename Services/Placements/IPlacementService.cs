
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Placements.DTOs;
using kanban_lia.Services.Placements.DTOs;

namespace kanban_lia.Services.Placements
{
    public interface IPlacementService
    {
        Task CreateAsync(CreatePlacementDto dto);
        Task<IEnumerable<PlacementDto>> GetCurrentAsync(GetPlacementDto dto);
        Task<IEnumerable<PlacementDto>> GetCurrentByBoardAsync(BoardId boardId);
    }
}