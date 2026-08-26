using kanban_lia.Models.Domain;

namespace kanban_lia.Infrastructure.Repositories
{
    public interface IPlacementRepository
    {
        Task CreateAsync(Placement placement);
        Task<Placement?> GetByIdAsync(Guid id);
    }
}
