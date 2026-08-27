using kanban_lia.Models.Domain.Placements;
using kanban_lia.Services.Placements.DTOs;


namespace kanban_lia.Services.Placements
{
    public interface IPlacementService
    {
        Task<Placement> CreateAsync(CreatePlacementDto dto);

        Task<Placement?> GetCurrentAsync(Guid id);
    }
}
