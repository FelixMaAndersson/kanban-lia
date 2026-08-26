using kanban_lia.Models.Domain;
using kanban_lia.Services.DTOs;

namespace kanban_lia.Services
{
    public interface IPlacementService
    {
        Task CreateAsync(CreatePlacementDto dto);

        Task<Placement?> GetByIdAsync(Guid id);
    }
}
