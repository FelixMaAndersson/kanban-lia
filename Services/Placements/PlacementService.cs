using AutoMapper;
using FractionalIndexing;

using kanban_lia.Hubs;
using kanban_lia.Infrastructure.Repositories.Boards;
using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Infrastructure.Repositories.Placements;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Exceptions;
using kanban_lia.Models.Domain.Placements;
using kanban_lia.Models.Domain.Placements.DTOs;
using kanban_lia.Models.Events;
using kanban_lia.Services.Boards.Exceptions;
using kanban_lia.Services.Columns.Exceptions;
using kanban_lia.Services.Placements.DTOs;
using Microsoft.AspNetCore.SignalR;
using static kanban_lia.Infrastructure.Schemas.Schema;

namespace kanban_lia.Services.Placements
{
    public class PlacementService(IPlacementRepository repository, IColumnRepository columnRepository, IBoardRepository boardRepository, IMapper mapper, IHubContext<BoardHub> hub) : IPlacementService
    {
        private readonly IPlacementRepository _repository = repository;
        private readonly IColumnRepository _columnRepository = columnRepository;
        private readonly IMapper _mapper = mapper;
        private readonly IHubContext<BoardHub> _hub = hub;
        private readonly IBoardRepository _boardRepository = boardRepository;

        public record PlacementCreatedEvent(
            Guid EntityId,
            Guid? SourceColumnId,
            Guid TargetColumnId
        );

        public async Task CreateAsync(IEnumerable<PlacementOperationDto> dtos)
        {
            foreach (var dto in dtos)
            {
                var entityIds = dto.Dto.EntityIds;

                var column = await _columnRepository.GetByIdAsync(dto.Dto.ColumnId);

                var board = await _boardRepository.GetByIdAsync(dto.Dto.BoardId);

                if (column is null)
                {
                    throw new ColumnNotFoundException(dto.Dto.ColumnId);
                }

                if (board is null)
                {
                    throw new BoardNotFoundException(dto.Dto.BoardId);
                }

                if (dto.Dto.AfterEntityIds.Any() && dto.Dto.BeforeEntityIds.Any())
                {
                    throw new InvalidDomainException(
                        "Only one of AfterEntityId and BeforeEntityId can be provided.");
                }

                if (column.BoardId != board.Id)
                {
                    throw new InvalidDomainException(
                        "The column does not belong to the specified board.");
                }

                EntityId? afterEntityId = null;
                EntityId? beforeEntityId = null;
                SortKeyLookup lookup;

                foreach (var entityId in dto.Dto.AfterEntityIds)
                {
                    var currentPlacements = await _repository.GetCurrentAsync(
                        [entityId],
                        board.Id
                        );

                    var placement = currentPlacements.FirstOrDefault();

                    if (placement?.ColumnId == column.Id)
                    {
                        afterEntityId = entityId;
                    }
                }

                if (afterEntityId is not null)
                {
                    lookup = SortKeyLookup.After;
                }

                else 
                {
                    foreach (var entityId in dto.Dto.BeforeEntityIds)
                    {
                        var currentPlacement = await _repository.GetCurrentAsync(
                            [entityId],
                            board.Id
                            );

                        var placement = currentPlacement.FirstOrDefault();

                        if (placement?.ColumnId == column.Id)
                        {
                            beforeEntityId = entityId;
                            break;
                        }
                    }

                    if (beforeEntityId is not null)
                    {
                        lookup = SortKeyLookup.Before;
                    }

                    else
                    {
                        lookup = SortKeyLookup.Last;
                    }
                }

                var range = await _repository.GetSortKeyRangeAsync(
                    column.Id,
                    lookup,
                    afterEntityId,
                    beforeEntityId);

                var placements = new List<Placement>();

                var previous = range.Previous;
                var next = range.Next;

                foreach (var entityId in entityIds)
                {

                    var sortKey = OrderKeyGenerator.GenerateKeyBetween(
                        previous,
                        next);

                    var placement = Placement.Create(
                        entityId,
                        board.Id,
                        column.Id,
                        sortKey);

                    placements.Add(placement);

                    previous = sortKey;
                }

                await _repository.CreateAsync(placements);
            }
        }

        public async Task<IEnumerable<PlacementDto>> GetCurrentAsync(GetPlacementDto dto)
        {
            var placements = await _repository.GetCurrentAsync(
                dto.EntityIds,
                dto.BoardId);

            return _mapper.Map<IEnumerable<PlacementDto>>(placements);
        }

        public async Task<IEnumerable<PlacementDto>> GetCurrentByColumnAsync(ColumnId columnId, BoardId boardId)
        {
            var placements = await _repository.GetCurrentByColumnAsync(
                columnId,
                boardId);
            return _mapper.Map<IEnumerable<PlacementDto>>(placements);
        }

        public async Task<IEnumerable<PlacementDto>> GetCurrentByBoardAsync(BoardId boardId)
        {
            var placements = await _repository.GetCurrentByBoardAsync(boardId);

            return _mapper.Map<IEnumerable<PlacementDto>>(placements);
        }
    }
}