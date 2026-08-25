using kanban_lia.Domain;
using kanban_lia.Infrastructure.Repositories;

namespace kanban_lia.Services
{
    public class PlacementService(PlacementRepository repo)
    {
        private readonly PlacementRepository _repository = repo;
        public async Task CreateAsync(CardPlacement placement)
        {
            await _repository.CreatePlacementAsync(placement);
        }
        public async Task<IEnumerable<CardPlacement>> GetAllPlacementsAsync()
        {
            return await _repository.GetAllPlacementsAsync();
        }
        public async Task<CardPlacement?> GetPlacementByIdAsync(Guid id)
        {
            return await _repository.GetPlacementByIdAsync(id);
        }
    }
}
