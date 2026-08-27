using kanban_lia.Infrastructure.Repositories.Placements;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;
using kanban_lia.Services.Placements.DTOs;

namespace kanban_lia.Services.Placements
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

        public async Task<Placement?> GetCurrentAsync(Guid id)
        {
            return await _repository.GetCurrentAsync(id);
        }
    }
}
