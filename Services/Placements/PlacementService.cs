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
using System.Transactions;

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

        public async Task<PlacementCreateResult> CreateAsync(
            IEnumerable<PlacementOperationDto> dtos,
            Guid correlationId,
            Guid? causationEventId,
            Actor actor,
            CancellationToken cancellationToken)
        {
            var operationsByEntity = GroupOperationsByEntity(dtos);
            var succeededEntityIds = new List<EntityId>();
            var failures = new List<PlacementFailure>();

            foreach (var entityGroup in operationsByEntity)
            {
                try
                {
                    await ProcessEntityGroupAsync(
                        entityGroup,
                        correlationId,
                        causationEventId,
                        actor,
                        cancellationToken);

                    succeededEntityIds.Add(entityGroup.EntityId);
                }
                catch (Exception exception) when (IsPlacementFailure(exception))
                {
                    failures.Add(new PlacementFailure(
                        entityGroup.EntityId,
                        exception.Message));
                }
            }

            return new PlacementCreateResult(succeededEntityIds, failures);
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

        //HELPER METHODS

        private static bool IsPlacementFailure(Exception exception)
        {
            return exception is ColumnNotFoundException
                or BoardNotFoundException
                or InvalidDomainException
                or ArgumentException
                || exception is Microsoft.Data.SqlClient.SqlException
                { Number: 547 or 2601 or 2627 };
        }

        private static EntityPlacementGroup[] GroupOperationsByEntity(
            IEnumerable<PlacementOperationDto> dtos)
        {
            var operations = dtos
                .Select(operation => operation with
                {
                    Dto = operation.Dto with
                    {
                        EntityIds = operation.Dto.EntityIds.ToArray(),
                        AfterEntityIds = operation.Dto.AfterEntityIds.ToArray(),
                        BeforeEntityIds = operation.Dto.BeforeEntityIds.ToArray()
                    },
                    SourceColumnIds = operation.SourceColumnIds.ToArray()
                })
                .ToArray();

            return operations
                .SelectMany(operation =>
                    operation.Dto.EntityIds.Select(entityId => new
                    {
                        EntityId = entityId,
                        Operation = operation with
                        {
                            Dto = operation.Dto with
                            {
                                EntityIds = [entityId]
                            }
                        }
                    }))
                .GroupBy(item => item.EntityId)
                .Select(group => new EntityPlacementGroup(
                    group.Key,
                    group.Select(item => item.Operation).ToArray()))
                .ToArray();
        }

        private async Task ProcessEntityGroupAsync(
            EntityPlacementGroup entityGroup,
            Guid correlationId,
            Guid? causationEventId,
            Actor actor,
            CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            using var transaction =
                await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                await AcquireSortKeyLocksAsync(
                    entityGroup,
                    transaction,
                    cancellationToken);

                foreach (var operation in entityGroup.Operations)
                {
                    await ProcessOperationAsync(
                        operation,
                        transaction,
                        correlationId,
                        causationEventId,
                        actor,
                        cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                // Rollback ska kunna köras även om anropets token har avbrutits.
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        }

        private async Task AcquireSortKeyLocksAsync(
            EntityPlacementGroup entityGroup,
            IDbTransaction transaction,
            CancellationToken cancellationToken)
        {
            var columnsToLock = entityGroup.Operations
                .Select(operation => operation.Dto.ColumnId)
                .Distinct()
                .OrderBy(columnId => columnId.Id);

            foreach (var columnId in columnsToLock)
            {
                await _repository.AcquireSortKeyLockAsync(
                    transaction,
                    columnId,
                    cancellationToken);
            }
        }

        private async Task ProcessOperationAsync(
            PlacementOperationDto operation,
            IDbTransaction transaction,
            Guid correlationId,
            Guid? causationEventId,
            Actor actor,
            CancellationToken cancellationToken)
        {
            var (column, board) = await ValidateAsync(operation);

            var range = await GetSortKeyRangeAsync(
                operation.Dto,
                transaction,
                cancellationToken);

            var placements = CreatePlacements(
                operation.Dto.EntityIds,
                board.Id,
                column.Id,
                range);

            await _repository.CreateAsync(placements, transaction);

            foreach (var placement in placements)
            {
                await AddPlacementCreatedEventAsync(
                    placement,
                    transaction,
                    correlationId,
                    causationEventId,
                    actor,
                    cancellationToken);
            }
        }

        private static List<Placement> CreatePlacements(
            IEnumerable<EntityId> entityIds,
            BoardId boardId,
            ColumnId columnId,
            SortKeyRange range)
        {
            var placements = new List<Placement>();
            var previous = range.Previous;
            var next = range.Next;

            foreach (var entityId in entityIds)
            {
                var sortKey =
                    OrderKeyGenerator.GenerateKeyBetween(previous, next);

                placements.Add(
                    Placement.Create(entityId, boardId, columnId, sortKey));

                previous = sortKey;
            }

            return placements;
        }

        private async Task AddPlacementCreatedEventAsync(
            Placement placement,
            IDbTransaction transaction,
            Guid correlationId,
            Guid? causationEventId,
            Actor actor,
            CancellationToken cancellationToken)
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
        /// Determines the sort-key range for inserting a new placement in the specified column based on adjacent entity
        /// identifiers.
        /// </summary>
        /// <remarks>Prefers an entity from dto.AfterEntityIds that is already in the target column to use
        /// an After lookup; if none is found, uses the first matching entity from dto.BeforeEntityIds for a Before
        /// lookup; otherwise defaults to Last. Invokes the repository to obtain the final sort-key range with the
        /// chosen lookup and adjacent entity identifiers.</remarks>
        /// <param name="dto">Placement creation details including BoardId, ColumnId, AfterEntityIds, and BeforeEntityIds used to locate
        /// adjacent placements.</param>
        /// <param name="transaction">The active database transaction used for repository operations.</param>
        /// <param name="cancellationToken">A CancellationToken to observe while performing asynchronous repository calls.</param>
        /// <returns>A SortKeyRange representing the computed range or position for the new placement's sort key within the
        /// column.</returns>
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
                    dto.BoardId,
                    transaction,
                    cancellationToken);

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
                        dto.BoardId,
                        transaction,
                        cancellationToken);

                    var placement = currentPlacements.FirstOrDefault();

                    if (placement?.ColumnId == dto.ColumnId)
                    {
                        beforeEntityId = entityId;
                        break;
                    }
                }

                lookup = beforeEntityId is not null
                    ? SortKeyLookup.Before
                    : SortKeyLookup.Last;
            }

            return await _repository.GetSortKeyRangeAsync(
                transaction,
                dto.ColumnId,
                lookup,
                afterEntityId,
                beforeEntityId,
                cancellationToken);
        }

        private sealed record EntityPlacementGroup(
            EntityId EntityId,
            PlacementOperationDto[] Operations);
    }
}