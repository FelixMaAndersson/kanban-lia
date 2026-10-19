using AutoMapper;
using FractionalIndexing;

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
using kanban_lia.Services.IntegrationEvents.Models;
using kanban_lia.Services.Placements.DTOs;
using System.Data;
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

        /// <summary>
        /// Create placements for the specified placement operations, persist them, and enqueue placement-created
        /// integration events in the outbox within a single database transaction per DTO.
        /// </summary>
        /// <remarks>Each DTO is processed sequentially. Positioning is determined by inspecting current
        /// placements on the board to choose a before/after/last lookup; order keys are generated between neighbors,
        /// placements are persisted, and corresponding outbox messages are enqueued within the same transaction.
        /// Transactions are rolled back on error.</remarks>
        /// <param name="dtos">Placement operations that contain entity ids, target board and column, and optional AfterEntityIds or
        /// BeforeEntityIds to determine ordering.</param>
        /// <param name="correlationId">Correlation identifier applied to produced integration events.</param>
        /// <param name="causationEventId">Optional causation event identifier included on produced integration events.</param>
        /// <param name="actor">Actor performing the operation and recorded on produced integration events.</param>
        /// <param name="cancellationToken">Token to cancel the operation and any underlying I/O.</param>
        /// <returns>A task that completes when all placements and corresponding outbox messages have been persisted and the
        /// transaction committed for each DTO.</returns>
        /// <exception cref="ColumnNotFoundException">Thrown when a referenced column cannot be found.</exception>
        /// <exception cref="BoardNotFoundException">Thrown when a referenced board cannot be found.</exception>
        /// <exception cref="InvalidDomainException">Thrown when both AfterEntityIds and BeforeEntityIds are provided, 
        /// or when the specified column does not belong to the specified board.</exception>
        public async Task CreateAsync(
            IEnumerable<PlacementOperationDto> dtos,
            Guid correlationId,
            Guid? causationEventId,
            Actor actor,
            CancellationToken cancellationToken)
        {

            foreach (var dto in dtos)
            {
                var entityIds = dto.Dto.EntityIds.ToArray();

                var (column, board) = await ValidateAsync(dto);

                using var connection = _connectionFactory.CreateConnection();

                await connection.OpenAsync(cancellationToken);

                using var transaction = await connection.BeginTransactionAsync(cancellationToken);

                try
                {
                    var columnsToLock = dto.SourceColumnIds
                        .Append(dto.Dto.ColumnId)
                        .Distinct()
                        .OrderBy(id => id.Id)
                        .ToArray();

                    foreach (var columnId in columnsToLock)
                    {
                        await _repository.AcquireSortKeyLockAsync(
                            transaction, columnId, cancellationToken);
                    }

                    var range = await GetSortKeyRangeAsync(
                        dto.Dto, transaction, cancellationToken);
                    var placements = new List<Placement>();
                    var previous = range.Previous;
                    var next = range.Next;

                    foreach (var entityId in entityIds)
                    {
                        var sortKey = OrderKeyGenerator.GenerateKeyBetween(previous, next);
                        placements.Add(Placement.Create(entityId, board.Id, column.Id, sortKey));
                        previous = sortKey;
                    }

                    await _repository.CreateAsync(
                        placements,
                        transaction);

                    foreach (var placement in placements)
                    {
                        var integrationEvent =
                            IntegrationEventFactory.CreatePlacementCreated(
                                entityId: placement.EntityId.Id,
                                columnId: placement.ColumnId.Id,
                                correlationId: correlationId,
                                causationEventId: causationEventId,
                                actor: actor);

                        var message = new OutboxMessage(
                            Id: integrationEvent.EventId,
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

                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
        }

        /// <summary>
        /// Retrieves current placements for the specified entities and board and 
        /// maps them to PlacementDto instances.
        /// </summary>
        /// <remarks>Fetches domain entities from the repository and maps them to DTOs using the
        /// configured mapper.</remarks>
        /// <param name="dto">A DTO containing the entity identifiers and the board identifier 
        /// used to filter current placements.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result contains an IEnumerable<PlacementDto>
        /// with the current placements matching the specified criteria.</returns>
        public async Task<IEnumerable<PlacementDto>> GetCurrentAsync(GetPlacementDto dto)
        {
            var placements = await _repository.GetCurrentAsync(
                dto.EntityIds,
                dto.BoardId);

            return _mapper.Map<IEnumerable<PlacementDto>>(placements);
        }

        /// <summary>
        /// Gets the current placements in the specified column of a board as PlacementDto objects.
        /// </summary>
        /// <param name="columnId">Identifier of the column whose current placements are retrieved.</param>
        /// <param name="boardId">Identifier of the board that contains the column.</param>
        /// <returns>A task that represents the asynchronous operation. 
        /// The task result contains the current placements in the
        /// specified column as an enumerable of PlacementDto.</returns>
        public async Task<IEnumerable<PlacementDto>> GetCurrentByColumnAsync(
            ColumnId columnId,
            BoardId boardId)
        {
            var placements = await _repository.GetCurrentByColumnAsync(
                columnId,
                boardId);
            return _mapper.Map<IEnumerable<PlacementDto>>(placements);
        }

        /// <summary>
        /// Gets the current placements for the specified board asynchronously.
        /// </summary>
        /// <param name="boardId">The identifier of the board whose current placements are retrieved.</param>
        /// <returns>A task representing the asynchronous operation. 
        /// The task result contains an enumerable of PlacementDto
        /// representing the current placements for the specified board.</returns>
        public async Task<IEnumerable<PlacementDto>> GetCurrentByBoardAsync(BoardId boardId)
        {
            var placements = await _repository.GetCurrentByBoardAsync(boardId);

            return _mapper.Map<IEnumerable<PlacementDto>>(placements);
        }

        // helper methods

        /// <summary>
        /// Validates that the specified column and board exist and that placement arguments are consistent.
        /// </summary>
        /// <param name="dto">PlacementOperationDto containing the column and board identifiers and optional placement position
        /// identifiers (AfterEntityIds/BeforeEntityIds).</param>
        /// <returns>A tuple containing the validated Column and Board.</returns>
        /// <exception cref="ColumnNotFoundException">Thrown when a column with the specified ID cannot be found.</exception>
        /// <exception cref="ColumnNotWritableException">Thrown when a column is not writable, hence the placement cannot be created inside of it.</exception>
        /// <exception cref="BoardNotFoundException">Thrown when a board with the specified ID cannot be found.</exception>
        /// <exception cref="InvalidDomainException">Thrown when the column does not belong to the specified board, or when both AfterEntityIds and
        /// BeforeEntityIds are provided.</exception>
        private async Task<(Column column, Board board)> ValidateAsync(PlacementOperationDto dto)
        {
            var column = await _columnRepository.GetByIdAsync(dto.Dto.ColumnId);

            var board = await _boardRepository.GetByIdAsync(dto.Dto.BoardId);

            if (column is null)
            {
                throw new ColumnNotFoundException(dto.Dto.ColumnId);
            }

            if (!column.RequestWritable)
            {
                throw new ColumnNotWritableException(column.Id);
            }

            if (board is null)
            {
                throw new BoardNotFoundException(dto.Dto.BoardId);
            }

            if (column.BoardId != board.Id)
            {
                throw new InvalidDomainException(
                    "The column does not belong to the specified board.");
            }

            if (dto.Dto.AfterEntityIds.Any() && dto.Dto.BeforeEntityIds.Any())
            {
                throw new InvalidDomainException(
                    "Only one of AfterEntityId and BeforeEntityId can be provided.");
            }

            return (column, board);

        }

        /// <summary>
        /// Determines the SortKeyRange for a new placement by inspecting adjacent placements in the specified column.
        /// </summary>
        /// <remarks>Checks AfterEntityIds first and uses the first entity currently in the same column to
        /// select a lookup of After. If no matching after-entity is found, checks BeforeEntityIds and uses the first
        /// matching entity to select a lookup of Before. If neither yields a neighbor, selects Last. The method queries
        /// the repository for current placements and delegates to the repository's GetSortKeyRangeAsync with the
        /// determined lookup and neighbor entity identifiers.</remarks>
        /// <param name="dto">CreatePlacementDto containing BoardId, ColumnId, and ordered AfterEntityIds and BeforeEntityIds used to find
        /// neighboring placements.</param>
        /// <returns>A SortKeyRange that defines the sort-key bounds to use when inserting the new placement.</returns>
        private async Task<SortKeyRange> GetSortKeyRangeAsync(
            CreatePlacementDto dto,
            IDbTransaction transaction,
            CancellationToken cancellationToken)
        {
            EntityId? afterEntityId = null;
            EntityId? beforeEntityId = null;
            SortKeyLookup lookup;

            foreach (var entityId in dto.AfterEntityIds)
            {
                var currentPlacements = await _repository.GetCurrentAsync(
                    [entityId],
                    dto.BoardId);

                var placement = currentPlacements.FirstOrDefault();

                if (placement?.ColumnId == dto.ColumnId)
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
                foreach (var entityId in dto.BeforeEntityIds)
                {
                    var currentPlacements = await _repository.GetCurrentAsync(
                        [entityId],
                        dto.BoardId);

                    var placement = currentPlacements.FirstOrDefault();

                    if (placement?.ColumnId == dto.ColumnId)
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

            return await _repository.GetSortKeyRangeAsync(
                transaction,
                dto.ColumnId,
                lookup,
                afterEntityId,
                beforeEntityId,
                cancellationToken);
        }
    }
}