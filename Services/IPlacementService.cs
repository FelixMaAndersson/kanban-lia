using kanban_lia.Models.Domain.Placements;
using kanban_lia.Services.DTOs;

namespace kanban_lia.Services
{
    public interface IPlacementService
    {
        Task<Placement> CreateAsync(CreatePlacementDto dto);

        Task<Placement?> GetByIdAsync(Guid id);
    }
}
