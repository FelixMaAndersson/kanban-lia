using AutoMapper;
using FractionalIndexing;

using kanban_lia.Hubs;
using kanban_lia.Infrastructure.Repositories.Boards;
using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Infrastructure.Repositories.Placements;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Exceptions;
using kanban_lia.Models.Domain.Placements;
using kanban_lia.Models.Domain.Placements.DTOs;
using kanban_lia.Services.Boards.Exceptions;
using kanban_lia.Services.Columns.Exceptions;
using kanban_lia.Services.Placements.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace kanban_lia.Services.Placements
{
    public class PlacementService(IPlacementRepository repository, IColumnRepository columnRepository, IBoardRepository boardRepository, IMapper mapper, IHubContext<BoardHub> hub) : IPlacementService
    {
        private readonly IPlacementRepository _repository = repository;
        private readonly IColumnRepository _columnRepository = columnRepository;
        private readonly IMapper _mapper = mapper;
        private readonly IHubContext<BoardHub> _hub = hub;
        private readonly IBoardRepository _boardRepository = boardRepository;

        public async Task CreateAsync(CreatePlacementDto dto)
        {
            var entityId = dto.EntityId;

            var column = await _columnRepository.GetByIdAsync(dto.ColumnId);

            var board = await _boardRepository.GetByIdAsync(dto.BoardId);

            if (column is null)
            {
                throw new ColumnNotFoundException(dto.ColumnId);
            }

            if (board is null)
            {
                throw new BoardNotFoundException(dto.BoardId);
            }

            if (dto.AfterEntityId.HasValue && dto.BeforeEntityId.HasValue)
            {
                throw new InvalidDomainException(
                    "Only one of AfterEntityId and BeforeEntityId can be provided.");
            }

            if (column.BoardId != board.Id)
            {
                throw new InvalidDomainException(
                    "The column does not belong to the specified board.");
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
                board.Id,
                column.Id,
                sortKey);

            await _repository.CreateAsync(placement);

            Console.WriteLine("sendning placement changed");

            await _hub.Clients.All.SendAsync("PlacementChanged");
        }

        public async Task<IEnumerable<PlacementDto>> GetCurrentAsync(GetPlacementDto dto)
        {
            var placements = await _repository.GetCurrentAsync(
                dto.EntityIds,
                dto.BoardId);

            return _mapper.Map<IEnumerable<PlacementDto>>(placements);
        }

        public async Task<IEnumerable<PlacementDto>> GetCurrentByBoardAsync(BoardId boardId)
        {
            var placements = await _repository.GetCurrentByBoardAsync(boardId);

            return _mapper.Map<IEnumerable<PlacementDto>>(placements);
        }
    }
}