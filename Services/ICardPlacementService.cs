using kanban_lia.Domain;

namespace kanban_lia.Services
{
    public interface ICardPlacementService
    {
        Task CreateAsync(CardPlacement placement);

        Task<IEnumerable<CardPlacement>> GetAllPlacementsAsync();

        Task<CardPlacement?> GetPlacementByIdAsync(Guid id);
    }
}
