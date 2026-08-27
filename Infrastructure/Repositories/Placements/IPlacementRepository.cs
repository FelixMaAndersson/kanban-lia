using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Infrastructure.Repositories.Placements
{
    public interface IPlacementRepository
    {
        Task<Placement> CreateAsync(Placement placement);
        Task<Placement?> GetByIdAsync(Guid id);
    }
}
