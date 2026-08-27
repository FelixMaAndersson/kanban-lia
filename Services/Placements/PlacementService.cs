using kanban_lia.Infrastructure.Repositories.Placements;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;
using kanban_lia.Services.Placements.DTOs;
using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Services.Placements.Exceptions;

namespace kanban_lia.Services.Placements
{
    public class PlacementService(IPlacementRepository repository, IColumnRepository columnRepository) : IPlacementService
    {
        private readonly IPlacementRepository _repository = repository;
        private readonly IColumnRepository _columnRepository = columnRepository;

        public async Task CreateAsync(CreatePlacementDto dto)
        {
            var entityId = new EntityId(dto.EntityId);
            var columnId = new ColumnId(dto.ColumnId);

     

            var column = await _columnRepository.GetByIdAsync(columnId);

            if (column is null)
            {
                throw new ColumnNotFoundException(columnId);
            }

            var newPlacement = Placement.Create(entityId, columnId, dto.Position);

            await _repository.CreateAsync(newPlacement);
        }

        public async Task<Placement?> GetCurrentAsync(Guid id)
        {
            return await _repository.GetCurrentAsync(id);
        }
    }
}
