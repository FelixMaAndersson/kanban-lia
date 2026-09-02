using AutoMapper;
using FractionalIndexing;
using kanban_lia.Hubs;
using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Infrastructure.Repositories.Placements;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Exceptions;
using kanban_lia.Models.Domain.Placements;
using kanban_lia.Models.Domain.Placements.DTOs;
using kanban_lia.Services.Columns.Exceptions;
using kanban_lia.Services.Placements.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace kanban_lia.Services.Placements
{
    public class PlacementService(IPlacementRepository repository, IColumnRepository columnRepository, IMapper mapper, IHubContext<BoardHub> hub) : IPlacementService
    {
        private readonly IPlacementRepository _repository = repository;
        private readonly IColumnRepository _columnRepository = columnRepository;
        private readonly IMapper _mapper = mapper;
        private readonly IHubContext<BoardHub> _hub = hub;

        public async Task CreateAsync(CreatePlacementDto dto)
        {
            var entityId = new EntityId(dto.EntityId);

            var column = await _columnRepository.GetByIdAsync(dto.ColumnId);

            if (column is null)
            {
                throw new ColumnNotFoundException(dto.ColumnId);
            }

            SortKeyLookup lookup;

            if (dto.AfterEntityId.HasValue)
            {
                lookup = SortKeyLookup.After;
            }
            else if (dto.BeforeEntityId.HasValue)
            {
                lookup = SortKeyLookup.Before;
            }
            else
            {
                lookup = SortKeyLookup.Last;
            }

            EntityId? afterEntityId;
            EntityId? beforeEntityId;

            if (dto.AfterEntityId.HasValue)
            {
                afterEntityId = new EntityId(dto.AfterEntityId.Value);
            }
            else
            {
                afterEntityId = null;
            }

            if (dto.BeforeEntityId.HasValue)
            {
                beforeEntityId = new EntityId(dto.BeforeEntityId.Value);
            }
            else
            {
                beforeEntityId = null;
            }

            if (dto.AfterEntityId.HasValue && dto.BeforeEntityId.HasValue)
            {
                throw new InvalidDomainException(
                    "Only one of AfterEntityId and BeforeEntityId can be provided.");
            }

            var range = await _repository.GetSortKeyRangeAsync(
                column.Id,
                lookup,
                afterEntityId,
                beforeEntityId);

            var sortKey = OrderKeyGenerator.GenerateKeyBetween(
                range.Previous,
                range.Next);

            var placement = Placement.Create(
                entityId,
                column.Id,
                sortKey);

            await _repository.CreateAsync(placement);

            Console.WriteLine("sendning placement changed");

            await _hub.Clients.All.SendAsync("PlacementChanged");
        }

        //public async Task<Placement?> GetCurrentAsync(Guid entityId, Guid boardId)
        //{
        //    return await _repository.GetCurrentAsync(new EntityId(entityId), new BoardId(boardId));
        //}

        public async Task<PlacementDto?> GetCurrentAsyncByColumn(Guid entityId, HashSet<ColumnId> columnIds)
        {
            var placement = await _repository.GetCurrentAsyncByColumn(new EntityId(entityId), columnIds);

            if (placement is null)
            {
                return null;
            }

            return _mapper.Map<PlacementDto>(placement);
        }
    }
}