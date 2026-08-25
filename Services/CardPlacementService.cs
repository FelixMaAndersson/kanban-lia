using kanban_lia.Domain;
using kanban_lia.Infrastructure.Repositories;

namespace kanban_lia.Services
{
    public class CardPlacementService: ICardPlacementService
    {
        private readonly CardPlacementRepository _repository;

        public CardPlacementService(CardPlacementRepository repository)
        {
            _repository = repository;
        }
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
