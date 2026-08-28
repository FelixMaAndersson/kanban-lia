using FractionalIndexing;
using kanban_lia.Infrastructure.Repositories.Placements;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;
using kanban_lia.Services.Placements.DTOs;
using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Services.Placements.Exceptions;
using kanban_lia.Models.Domain.Boards;

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

            string? previousSortKey = null;
            string? nextSortKey = null;


            if (dto.AfterEntityId.HasValue)
            {
                var afterEntityId = new EntityId(dto.AfterEntityId.Value);

                previousSortKey = await _repository.GetCurrentSortKeyAsync(
                    afterEntityId,
                    columnId);

                if (previousSortKey is null)
                {
                    throw new PlacementNotFoundException(afterEntityId);
                }

                nextSortKey = await _repository.GetNextSortKeyAsync(
                    previousSortKey,
                    columnId
                    );
            }
            else
            {
                nextSortKey = await _repository.GetFirstSortKeyAsync(columnId);
            }

            var sortKey = OrderKeyGenerator.GenerateKeyBetween(previousSortKey, nextSortKey);

            var newPlacement = Placement.Create(entityId, columnId, sortKey);

            await _repository.CreateAsync(newPlacement);
        }

        public async Task<Placement?> GetCurrentAsync(Guid entityId, Guid boardId)
        {
            return await _repository.GetCurrentAsync(new EntityId(entityId),
            new BoardId(boardId));
        }
    }
}
