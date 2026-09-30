using AutoMapper;
using FractionalIndexing;

using kanban_lia.Hubs;
using kanban_lia.Infrastructure.Database;
using kanban_lia.Infrastructure.Repositories.Boards;
using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Infrastructure.Repositories.Outbox;
using kanban_lia.Infrastructure.Repositories.Placements;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Exceptions;
using kanban_lia.Models.Domain.Placements;
using kanban_lia.Models.Domain.Placements.DTOs;
using kanban_lia.Services.Boards.Exceptions;
using kanban_lia.Services.Columns.Exceptions;
using kanban_lia.Services.IntegrationEvents;
using kanban_lia.Services.Placements.DTOs;
using System.Text.Json;

namespace kanban_lia.Services.Placements
{
    public class PlacementService(
        DbConnectionFactory connectionFactory,
        IBoardRepository boardRepository,
        IColumnRepository columnRepository,
        IPlacementRepository repository,
        IOutboxRepository outboxRepository,
        IMapper mapper) : IPlacementService
    {
        private readonly DbConnectionFactory _connectionFactory = connectionFactory;
        private readonly IBoardRepository _boardRepository = boardRepository;
        private readonly IColumnRepository _columnRepository = columnRepository;
        private readonly IPlacementRepository _repository = repository;
        private readonly IOutboxRepository _outboxRepository = outboxRepository;
        private readonly IMapper _mapper = mapper;

        public record PlacementCreatedEvent(
            Guid EntityId,
            Guid? SourceColumnId,
            Guid TargetColumnId
        );

        public async Task CreateAsync(
            IEnumerable<PlacementOperationDto> dtos,
            Guid? correlationId,
            Guid? causationEventId,
            CancellationToken cancellationToken)
        {
            foreach (var dto in dtos)
            {
                var entityIds = dto.Dto.EntityIds.ToArray();

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

                using var connection = _connectionFactory.CreateConnection();

                connection.Open();

                using var transaction = connection.BeginTransaction();

                try
                {
                    await _repository.CreateAsync(
                        placements,
                        transaction);

                    foreach (var placement in placements)
                    {
                        var integrationEvent =
                            IntegrationEventFactory.CreatePlacementCreated(
                                entityId: placement.EntityId.Id,
                                columnId: placement.ColumnId.Id,
                                correlationId: correlationId ?? Guid.NewGuid(),
                                causationEventId: causationEventId);

                        var message = new OutboxMessage(
                            Id: Guid.Parse(integrationEvent.EventId),
                            EventType: integrationEvent.EventType,
                            Content: JsonSerializer.Serialize(integrationEvent),
                            OccurredOn: DateTime.UtcNow,
                            ProcessedOn: null,
                            Error: null);

                        await _outboxRepository.AddAsync(
                            message,
                            transaction,
                            cancellationToken);
                    }

                    transaction.Commit();

                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public async Task<IEnumerable<PlacementDto>> GetCurrentAsync(GetPlacementDto dto)
        {
            var placements = await _repository.GetCurrentAsync(
                dto.EntityIds,
                dto.BoardId);

            return _mapper.Map<IEnumerable<PlacementDto>>(placements);
        }

        public async Task<IEnumerable<PlacementDto>> GetCurrentByColumnAsync(
            ColumnId columnId,
            BoardId boardId)
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