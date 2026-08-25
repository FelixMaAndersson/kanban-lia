using kanban_lia.Domain;
using kanban_lia.Infrastructure.Repositories;

namespace kanban_lia.Services
{
    public class PlacementService(PlacementRepository repository) : IPlacementService
    {
        private readonly PlacementRepository _repository = repository;

        public async Task CreateAsync(Placement placement)
        {
            await _repository.CreateAsync(placement);
        }
        public async Task<IEnumerable<Placement>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }
        public async Task<Placement?> GetByIdAsync(Guid id)
        {
            return await _repository.GetByIdAsync(id);
        }
    }
}
