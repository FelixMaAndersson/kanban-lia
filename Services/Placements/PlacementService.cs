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

            var column = await _columnRepository.GetByIdAsync(dto.ColumnId);

            if (column is null)
            {
                throw new ColumnNotFoundException(dto.ColumnId);
            }

            var lookup = dto.AfterEntityId.HasValue
                ? SortKeyLookup.After
                : SortKeyLookup.First;

            var range = await _repository.GetSortKeyRangeAsync(
                column.Id,
                lookup,
                dto.AfterEntityId.HasValue
                    ? new EntityId(dto.AfterEntityId.Value)
                    : null);

            var sortKey = OrderKeyGenerator.GenerateKeyBetween(
                range.Previous,
                range.Next);

            var placement = Placement.Create(
                entityId,
                column.Id,
                sortKey);

            await _repository.CreateAsync(placement);
        }

        public async Task<Placement?> GetCurrentAsync(Guid entityId, Guid boardId)
        {
            return await _repository.GetCurrentAsync(new EntityId(entityId),
            new BoardId(boardId));
        }
    }
}
