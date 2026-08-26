using kanban_lia.Infrastructure.Repositories;
using kanban_lia.Models.Domain;
using kanban_lia.Services.DTOs;

namespace kanban_lia.Services
{
    public class PlacementService(IPlacementRepository repository) : IPlacementService
    {
        private readonly IPlacementRepository _repository = repository;

        public async Task<Placement> CreateAsync(CreatePlacementDto dto)
        {
            var entityId = new EntityId(dto.EntityId);
            var columnId = new ColumnId(dto.ColumnId);

            var newPlacement = Placement.Create(entityId, columnId, dto.Position);

            return await _repository.CreateAsync(newPlacement);
        }

        public async Task<Placement?> GetByIdAsync(Guid id)
        {
            return await _repository.GetByIdAsync(id);
        }
    }
}
