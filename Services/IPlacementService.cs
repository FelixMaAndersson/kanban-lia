using kanban_lia.Domain;

namespace kanban_lia.Services
{
    public interface IPlacementService
    {
        Task CreateAsync(Placement placement);

        Task<IEnumerable<Placement>> GetAllAsync();

        Task<Placement?> GetByIdAsync(Guid id);
    }
}
