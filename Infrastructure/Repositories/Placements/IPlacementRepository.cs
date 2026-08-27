using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Infrastructure.Repositories.Placements
{
    public interface IPlacementRepository
    {
        Task CreateAsync(Placement placement);
        Task<Placement?> GetCurrentAsync(Guid id);
    }
}
