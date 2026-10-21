using AutoMapper;

using Microsoft.Extensions.Logging.Abstractions;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

using kanban_lia.Infrastructure.Repositories.Boards;
using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Infrastructure.Repositories.Outbox;
using kanban_lia.Infrastructure.Repositories.Placements;
using kanban_lia.Integration.Tests.Fixture;
using kanban_lia.Mappings;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;
using kanban_lia.Services.Boards;
using kanban_lia.Services.Columns;
using kanban_lia.Services.Columns.DTOs;
using kanban_lia.Services.Columns.Exceptions;
using kanban_lia.Services.IntegrationEvents.Models;
using kanban_lia.Services.Placements;
using kanban_lia.Services.Placements.DTOs;

namespace kanban_lia.Integration.Tests.Services
{
    /// <summary>
    /// Class <c>PlacementServiceTests</c> is the test class for <c>PlacementService</c>
    /// </summary>
    /// <param name="database">Primary constructor for <c>DatabaseFixture</c></param>
    [Collection(DatabaseCollection.Name)]
    public sealed class PlacementServiceTests(DatabaseFixture database) : IAsyncLifetime
    {
        private readonly DatabaseFixture _database = database;

        private static readonly IMapper _mapper = new MapperConfiguration(
            cfg =>
            {
                cfg.AddProfile(new BoardProfile());
                cfg.AddProfile(new ColumnProfile());
                cfg.AddProfile(new PlacementProfile());
            },
            NullLoggerFactory.Instance).CreateMapper();

        private readonly BoardService _boardService = new(
            new BoardRepository(database.DbFactory),
            _mapper);

        private readonly ColumnService _columnService = new(
            new ColumnRepository(database.DbFactory),
            new BoardRepository(database.DbFactory),
            new ColumnEdgeRepository(database.DbFactory),
            _mapper);

        private readonly ColumnEdgeService _columnEdgeService = new(
            new ColumnEdgeRepository(database.DbFactory),
            _mapper);

        private readonly PlacementService _placementService = new(
            database.DbFactory,
            new BoardRepository(database.DbFactory),
            new ColumnRepository(database.DbFactory),
            new PlacementRepository(database.DbFactory),
            new OutboxRepository(database.DbFactory),
            _mapper);

        private readonly PlacementRepository _repository = new(database.DbFactory);

        public async ValueTask InitializeAsync()
        {
            await _database.ResetAsync();
        }

        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }

        [Fact]
        public async Task GetCurrentAsync_ReturnsLatestPerEntity()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Testboard");
            var columnId1 = new ColumnId(Guid.NewGuid());
            var columnId2 = new ColumnId(Guid.NewGuid());
            var entityId = new EntityId(Guid.NewGuid());

            await _columnService.CreateAsync(
                new CreateColumnDto(columnId1, "Column 1", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            await _columnService.CreateAsync(
                new CreateColumnDto(columnId2, "Column 2", 2, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            var operation1 = new PlacementOperationDto(
                new CreatePlacementDto([entityId], boardId, columnId1, [], []),
                []);
            var operation2 = new PlacementOperationDto(
                new CreatePlacementDto([entityId], boardId, columnId2, [], []),
                []);

            // Act
            var firstResult = await _placementService.CreateAsync(
                [operation1],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                cancellationToken);

            var secondResult = await _placementService.CreateAsync(
                [operation2],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                cancellationToken);

            // Assert
            Assert.Contains(entityId, firstResult.SucceededEntityIds);
            Assert.Empty(firstResult.Failures);
            Assert.Contains(entityId, secondResult.SucceededEntityIds);
            Assert.Empty(secondResult.Failures);
            var placements = await _repository.GetCurrentAsync([entityId], boardId);
            Assert.Equal(columnId2, Assert.Single(placements).ColumnId);
        }

        [Fact]
        public async Task GetCurrentByColumnAsync_SortedByBinaryCollation()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Testboard");
            var columnId = new ColumnId(Guid.NewGuid());

            await _columnService.CreateAsync(
                new CreateColumnDto(columnId, "Column 1", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            var entityIds = Enumerable.Range(0, 10)
                .Select(_ => new EntityId(Guid.NewGuid()))
                .ToArray();

            var operations = entityIds.Select(entityId =>
                new PlacementOperationDto(
                    new CreatePlacementDto([entityId], boardId, columnId, [], []),
                    [])).ToArray();
            
            // Act
            var result = await _placementService.CreateAsync(
                operations,
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                cancellationToken);
            
            // Assert
            var placements = (await _repository.GetCurrentByColumnAsync(columnId, boardId))
                .ToArray();

            Assert.Equal(entityIds, result.SucceededEntityIds);
            Assert.Empty(result.Failures);

            Assert.Equal(entityIds.Length, placements.Length);
            Assert.Equal(
                placements.Length,
                placements.Select(placement => placement.SortKey).Distinct().Count());
            
            for (var i = 1; i < placements.Length; i++)
            {
                Assert.True(
                    string.CompareOrdinal(
                        placements[i - 1].SortKey,
                        placements[i].SortKey) < 0,
                    $"Expected placement {i} to be after placement {i - 1}. " +
                    $"placement {i - 1} SortKey: '{placements[i - 1].SortKey}', " +
                    $"placement {i} SortKey: '{placements[i].SortKey}'");
            }
        }


        [Fact]
        public async Task GetCurrentByBoardAsync_GroupsByColumn()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Testboard");
            var columnId1 = new ColumnId(Guid.NewGuid());
            var columnId2 = new ColumnId(Guid.NewGuid());
            
            await _columnService.CreateAsync(
                new CreateColumnDto(columnId1, "Column 1", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);
            
            await _columnService.CreateAsync(
                new CreateColumnDto(columnId2, "Column 2", 2, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);
            
            var entityIdsColumn1 = Enumerable.Range(0, 5)
                .Select(_ => new EntityId(Guid.NewGuid()))
                .ToArray();         
            var entityIdsColumn2 = Enumerable.Range(0, 5)
                .Select(_ => new EntityId(Guid.NewGuid()))
                .ToArray();
            
            var operationsColumn1 = entityIdsColumn1.Select(entityId =>
                new PlacementOperationDto(
                    new CreatePlacementDto([entityId], boardId, columnId1, [], []),
                    [])).ToArray();
            var operationsColumn2 = entityIdsColumn2.Select(entityId =>
                new PlacementOperationDto(
                    new CreatePlacementDto([entityId], boardId, columnId2, [], []),
                    [])).ToArray();
            
            // Act
            await _placementService.CreateAsync(
                operationsColumn1,
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                cancellationToken);
            await _placementService.CreateAsync(
                operationsColumn2,
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                cancellationToken);

            // Assert
            var placementsByBoard = (await _repository.GetCurrentByBoardAsync(boardId))
                .GroupBy(placement => placement.ColumnId)
                .ToDictionary(group => group.Key, group => group.ToList());
            Assert.Equal(2, placementsByBoard.Count);

            Assert.Equal(
                entityIdsColumn1,
                placementsByBoard[columnId1].Select(placement => placement.EntityId));

            Assert.Equal(
                entityIdsColumn2,
                placementsByBoard[columnId2].Select(placement => placement.EntityId));

            Assert.Equal(entityIdsColumn1.Length, placementsByBoard[columnId1].Count);
            Assert.Equal(entityIdsColumn2.Length, placementsByBoard[columnId2].Count);
        }

        [Fact]
        public async Task CreateAsync_WithAfterEntityId_UsesAfterLookup()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Testboard");
            var columnId = new ColumnId(Guid.NewGuid());

            await _columnService.CreateAsync(
                new CreateColumnDto(
                    columnId,
                    "Column 1",
                    1,
                    boardId,
                    true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            var operation1EntityId = new EntityId(Guid.NewGuid());
            var operation2EntityId = new EntityId(Guid.NewGuid());

            var operation1 = new PlacementOperationDto(
                new CreatePlacementDto(
                    [operation1EntityId],
                    boardId,
                    columnId,
                    [],
                    []),
                []);

            var operation2 = new PlacementOperationDto(
                new CreatePlacementDto(
                    [operation2EntityId],
                    boardId,
                    columnId,
                    [operation1EntityId],
                    []),
                []);

            await _placementService.CreateAsync(
                [operation1],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                cancellationToken);

            // Act
            await _placementService.CreateAsync(
                [operation2],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                cancellationToken);

            // Assert
            var placement1 = await _repository.GetCurrentAsync(
                [operation1EntityId],
                boardId);

            var placement2 = await _repository.GetCurrentAsync(
                [operation2EntityId],
                boardId);

            Assert.NotNull(placement1);
            Assert.NotNull(placement2);

            Assert.True(
                string.CompareOrdinal(
                    placement1.First().SortKey,
                    placement2.First().SortKey) < 0,
                $"Expected entity2 to be after entity1. " +
                $"entity1 SortKey: '{placement1.First().SortKey}', " +
                $"entity2 SortKey: '{placement2.First().SortKey}'");
        }

        [Fact]
        public async Task CreateAsync_ConcurrentInsertionsAfterSameEntity_UseDistinctSortKeys()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Concurrent placement test");
            var columnId = new ColumnId(Guid.NewGuid());

            await _columnService.CreateAsync(
                new CreateColumnDto(columnId, "Column 1", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("test-user", "User"),
                cancellationToken);

            var anchorEntityId = new EntityId(Guid.NewGuid());
            var anchorOperation = new PlacementOperationDto(
                new CreatePlacementDto(
                    [anchorEntityId], boardId, columnId, [], []),
                []);

            await _placementService.CreateAsync(
                [anchorOperation], Guid.NewGuid(), null,
                new Actor("test-user", "User"), cancellationToken);

            var insertions = Enumerable.Range(0, 12).Select(_ =>
            {
                var entityId = new EntityId(Guid.NewGuid());
                var operation = new PlacementOperationDto(
                    new CreatePlacementDto(
                        [entityId], boardId, columnId, [anchorEntityId], []),
                    []);

                return _placementService.CreateAsync(
                    [operation], Guid.NewGuid(), null,
                    new Actor("test-user", "User"), cancellationToken);
            });

            // Act
            await Task.WhenAll(insertions);

            // Assert
            var placements = await _repository.GetCurrentByColumnAsync(columnId, boardId);
            var sortKeys = placements.Select(placement => placement.SortKey).ToArray();

            Assert.Equal(13, sortKeys.Length);
            Assert.Equal(sortKeys.Length, sortKeys.Distinct().Count());
        }

        [Fact]
        public async Task CreateAsync_WithBeforeEntityId_UsesBeforeLookup()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Testboard");
            var columnId = new ColumnId(Guid.NewGuid());

            await _columnService.CreateAsync(
                new CreateColumnDto(
                    columnId,
                    "Column 1",
                    1,
                    boardId,
                    true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            var operation1EntityId = new EntityId(Guid.NewGuid());
            var operation2EntityId = new EntityId(Guid.NewGuid());

            var operation1 = new PlacementOperationDto(
                new CreatePlacementDto(
                    [operation1EntityId],
                    boardId,
                    columnId,
                    [],
                    []),
                []);

            var operation2 = new PlacementOperationDto(
                new CreatePlacementDto(
                    [operation2EntityId],
                    boardId,
                    columnId,
                    [],
                    [operation1EntityId]),
                []);

            await _placementService.CreateAsync(
                [operation1],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                cancellationToken);

            // Act
            await _placementService.CreateAsync(
                [operation2],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                cancellationToken);

            // Assert
            var placement1 = await _repository.GetCurrentAsync(
                [operation1EntityId],
                boardId);

            var placement2 = await _repository.GetCurrentAsync(
                [operation2EntityId],
                boardId);

            Assert.NotNull(placement1);
            Assert.NotNull(placement2);

            Assert.True(
                string.CompareOrdinal(
                    placement1.First().SortKey,
                    placement2.First().SortKey) > 0,
                $"Expected entity2 to be before entity1. " +
                $"entity1 SortKey: '{placement1.First().SortKey}', " +
                $"entity2 SortKey: '{placement2.First().SortKey}'");
        }

        [Fact]
        public async Task CreateAsync_WithNoAfterOrBeforeEntityId_UsesEmptyLookup()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Testboard");
            var columnId = new ColumnId(Guid.NewGuid());

            await _columnService.CreateAsync(
                new CreateColumnDto(
                    columnId,
                    "Column 1",
                    1,
                    boardId,
                    true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            // Act
            var operation = new PlacementOperationDto(
                new CreatePlacementDto(
                    [new(Guid.NewGuid())],
                    boardId,
                    columnId,
                    [],
                    []),
                []);

            await _placementService.CreateAsync(
                [operation],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                cancellationToken);

            // Assert
            var placement = await _repository.GetCurrentAsync(
                [new(Guid.NewGuid())],
                boardId);

            Assert.NotNull(placement);
        }

        [Fact]
        public async Task CreateAsync_WithBothBeforeAndAfter_ReturnsFailureForEntity()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Testboard");
            var columnId = new ColumnId(Guid.NewGuid());
            var entityId = new EntityId(Guid.NewGuid());

            await _columnService.CreateAsync(
                new CreateColumnDto(
                    columnId,
                    "Column 1",
                    1,
                    boardId,
                    true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            var operation = new PlacementOperationDto(
                new CreatePlacementDto(
                    [entityId],
                    boardId,
                    columnId,
                    [new EntityId(Guid.NewGuid())],
                    [new EntityId(Guid.NewGuid())]),
                []);

            // Act
            var result = await _placementService.CreateAsync(
                [operation],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                cancellationToken);

            // Assert
            Assert.Empty(result.SucceededEntityIds);

            var failure = Assert.Single(result.Failures);
            Assert.Equal(entityId, failure.EntityId);
            Assert.Contains("Only one", failure.Error);
        }

        [Fact]
        public async Task CreateAsync_WithoutBeforeOrAfter_WhenColumnHasPlacements_PlacesLast()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Testboard");
            var columnId = new ColumnId(Guid.NewGuid());

            await _columnService.CreateAsync(
                new CreateColumnDto(
                    columnId,
                    "Column 1",
                    1,
                    boardId,
                    true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            var operation1EntityId = new EntityId(Guid.NewGuid());
            var operation2EntityId = new EntityId(Guid.NewGuid());

            var operation1 = new PlacementOperationDto(
                new CreatePlacementDto(
                    [operation1EntityId],
                    boardId,
                    columnId,
                    [],
                    []),
                []);

            var operation2 = new PlacementOperationDto(
                new CreatePlacementDto(
                    [operation2EntityId],
                    boardId,
                    columnId,
                    [],
                    []),
                []);

            await _placementService.CreateAsync(
                [operation1],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                cancellationToken);

            // Act
            await _placementService.CreateAsync(
                [operation2],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                cancellationToken);

            // Assert
            var placement1 = await _repository.GetCurrentAsync(
                [operation1EntityId],
                boardId);

            var placement2 = await _repository.GetCurrentAsync(
                [operation2EntityId],
                boardId);

            Assert.NotNull(placement1);
            Assert.NotNull(placement2);

            Assert.NotEqual(
                placement1.First().SortKey,
                placement2.First().SortKey);

            Assert.True(
                string.CompareOrdinal(
                    placement1.First().SortKey,
                    placement2.First().SortKey) < 0,
                $"Expected entity2 to be placed after entity1. " +
                $"entity1 SortKey: '{placement1.First().SortKey}', " +
                $"entity2 SortKey: '{placement2.First().SortKey}'");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CreateAsync_WhenAnchorIsInAnotherColumn_PlacesLast(bool useAfterAnchor)
        {
            var boardId = await _boardService.CreateAsync("Anchor fallback test");
            var targetColumnId = new ColumnId(Guid.NewGuid());
            var otherColumnId = new ColumnId(Guid.NewGuid());

            await _columnService.CreateAsync(
                new CreateColumnDto(targetColumnId, "Target", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                CancellationToken.None);

            await _columnService.CreateAsync(
                new CreateColumnDto(otherColumnId, "Other", 2, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                CancellationToken.None);

            var existingEntityId = new EntityId(Guid.NewGuid());
            var anchorEntityId = new EntityId(Guid.NewGuid());
            var newEntityId = new EntityId(Guid.NewGuid());

            async Task CreatePlacementAsync(EntityId entityId, ColumnId columnId)
            {
                var operation = new PlacementOperationDto(
                    new CreatePlacementDto(
                        [entityId],
                        boardId,
                        columnId,
                        [],
                        []),
                    []);

                await _placementService.CreateAsync(
                    [operation],
                    Guid.NewGuid(),
                    null,
                    new Actor("-1", "TestActor"),
                    CancellationToken.None);
            }

            await CreatePlacementAsync(existingEntityId, targetColumnId);
            await CreatePlacementAsync(anchorEntityId, otherColumnId);

            var afterEntityIds = useAfterAnchor
                ? [anchorEntityId]
                : Array.Empty<EntityId>();
            var beforeEntityIds = useAfterAnchor
                ? Array.Empty<EntityId>()
                : [anchorEntityId];

            var operationWithMissingTargetAnchor = new PlacementOperationDto(
                new CreatePlacementDto(
                    [newEntityId],
                    boardId,
                    targetColumnId,
                    afterEntityIds,
                    beforeEntityIds),
                []);

            await _placementService.CreateAsync(
                [operationWithMissingTargetAnchor],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                CancellationToken.None);

            var placements = (await _repository.GetCurrentByColumnAsync(
                targetColumnId,
                boardId)).ToList();

            Assert.Equal(2, placements.Count);
            Assert.Equal(existingEntityId, placements[0].EntityId);
            Assert.Equal(newEntityId, placements[1].EntityId);
        }

        [Fact]
        public async Task CreateAsync_WhenColumnBelongsToDifferentBoard_ReturnsFailureForEntity()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var requestedBoardId = await _boardService.CreateAsync("Requested board");
            var columnOwnerBoardId = await _boardService.CreateAsync("Column owner board");
            var columnId = new ColumnId(Guid.NewGuid());
            var entityId = new EntityId(Guid.NewGuid());

            await _columnService.CreateAsync(
                new CreateColumnDto(
                    columnId,
                    "Column on another board",
                    1,
                    columnOwnerBoardId,
                    true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            var operation = new PlacementOperationDto(
                new CreatePlacementDto(
                    [entityId],
                    requestedBoardId,
                    columnId,
                    [],
                    []),
                []);

            // Act
            var result = await _placementService.CreateAsync(
                [operation],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                cancellationToken);

            // Assert
            Assert.Empty(result.SucceededEntityIds);

            var failure = Assert.Single(result.Failures);
            Assert.Equal(entityId, failure.EntityId);
            Assert.Contains("does not belong", failure.Error);
        }

        [Fact]
        public async Task CreateAsync_WithNonExistingColumn_ReturnsFailureForEntity()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Testboard");
            var columnId = new ColumnId(Guid.NewGuid());
            var entityId = new EntityId(Guid.NewGuid());

            var operation = new PlacementOperationDto(
                new CreatePlacementDto(
                    [entityId],
                    boardId,
                    columnId,
                    [],
                    []),
                []);

            // Act
            var result = await _placementService.CreateAsync(
                [operation],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                cancellationToken);

            // Assert
            Assert.Empty(result.SucceededEntityIds);

            var failure = Assert.Single(result.Failures);
            Assert.Equal(entityId, failure.EntityId);
            Assert.Contains(columnId.Id.ToString(), failure.Error);
        }

        [Fact]
        public async Task CreateAsync_WhenLaterOperationForEntityFails_RollsBackEarlierPlacementAndOutboxMessage()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var firstBoardId = await _boardService.CreateAsync("First board");
            var secondBoardId = await _boardService.CreateAsync("Second board");
            var firstColumnId = new ColumnId(Guid.NewGuid());
            var entityId = new EntityId(Guid.NewGuid());
            var outboxRepository = new OutboxRepository(_database.DbFactory);

            await _columnService.CreateAsync(
                new CreateColumnDto(
                    firstColumnId,
                    "First column",
                    1,
                    firstBoardId,
                    true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            var outboxBefore = (await outboxRepository
                .GetUnprocessedAsync(cancellationToken))
                .Select(message => message.Id)
                .OrderBy(id => id)
                .ToArray();

            var validOperation = new PlacementOperationDto(
                new CreatePlacementDto(
                    [entityId],
                    firstBoardId,
                    firstColumnId,
                    [],
                    []),
                []);

            var failingOperation = new PlacementOperationDto(
                new CreatePlacementDto(
                    [entityId],
                    secondBoardId,
                    firstColumnId,
                    [],
                    []),
                []);

            // Act
            var result = await _placementService.CreateAsync(
                [validOperation, failingOperation],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                cancellationToken);

            // Assert
            Assert.Empty(result.SucceededEntityIds);

            var failure = Assert.Single(result.Failures);
            Assert.Equal(entityId, failure.EntityId);

            var placement = await _repository.GetCurrentAsync(
                [entityId],
                firstBoardId);

            Assert.Empty(placement);

            var outboxAfter = (await outboxRepository
                .GetUnprocessedAsync(cancellationToken))
                .Select(message => message.Id)
                .OrderBy(id => id)
                .ToArray();

            Assert.Equal(outboxBefore, outboxAfter);
        }

        [Fact]
        public async Task CreateAsync_WhenSourceColumnIsNotWritable_MovesCardOut()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Drag out board");
            var archiveColumnId = new ColumnId(Guid.NewGuid());
            var doingColumnId = new ColumnId(Guid.NewGuid());
            var entityId = new EntityId(Guid.NewGuid());

            await _columnService.CreateAsync(
                new CreateColumnDto(archiveColumnId, "Archive", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            await _columnService.CreateAsync(
                new CreateColumnDto(doingColumnId, "Doing", 2, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            await _placementService.CreateAsync(
                [new PlacementOperationDto(
                    new CreatePlacementDto([entityId], boardId, archiveColumnId, [], []),
                    [])],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                cancellationToken,
                boardId);

            await _columnService.SetRequestWritableAsync(archiveColumnId, false);

            // Act
            var result = await _placementService.CreateAsync(
                [new PlacementOperationDto(
                    new CreatePlacementDto([entityId], boardId, doingColumnId, [], []),
                    [archiveColumnId])],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                cancellationToken,
                boardId);

            // Assert
            Assert.Empty(result.Failures);
            Assert.Contains(entityId, result.SucceededEntityIds);

            var current = await _repository.GetCurrentAsync([entityId], boardId);
            Assert.Equal(doingColumnId, Assert.Single(current).ColumnId);

            Assert.Empty(await _repository.GetCurrentByColumnAsync(archiveColumnId, boardId));
        }

        [Fact]
        public async Task CreateAsync_WhenRequestingBoardTargetsOwnReadOnlyColumn_ThrowsColumnNotWritableException()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Direct deny board");
            var rejectedColumnId = new ColumnId(Guid.NewGuid());
            var entityId = new EntityId(Guid.NewGuid());

            await _columnService.CreateAsync(
                new CreateColumnDto(rejectedColumnId, "Rejected", 1, boardId, false),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            var operation = new PlacementOperationDto(
                new CreatePlacementDto([entityId], boardId, rejectedColumnId, [], []),
                []);

            // Act
            await Assert.ThrowsAsync<ColumnNotWritableException>(() =>
                _placementService.CreateAsync(
                    [operation],
                    Guid.NewGuid(),
                    null,
                    new Actor("-1", "TestActor"),
                    cancellationToken,
                    boardId));

            // Assert
            Assert.Empty(await _repository.GetCurrentByColumnAsync(rejectedColumnId, boardId));
            Assert.Empty(await _repository.GetCurrentAsync([entityId], boardId));
        }

        [Fact]
        public async Task CreateAsync_WhenAnotherBoardSendsViaEdge_AllowsIntoReadOnlyColumn()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var setup = await CreateConnectedRejectColumnsAsync(cancellationToken);
            var entityId = new EntityId(Guid.NewGuid());

            var operations = CreateRejectFanOutOperations(entityId, setup);

            // Act
            var result = await _placementService.CreateAsync(
                operations,
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                cancellationToken,
                setup.TestingBoardId);

            // Assert
            Assert.Empty(result.Failures);
            Assert.Contains(entityId, result.SucceededEntityIds);

            var inTesting = await _repository.GetCurrentByColumnAsync(
                setup.TestingRejectId, setup.TestingBoardId);
            Assert.Equal(entityId, Assert.Single(inTesting).EntityId);

            var inDevelopment = await _repository.GetCurrentByColumnAsync(
                setup.DevelopmentRejectedId, setup.DevelopmentBoardId);
            Assert.Equal(entityId, Assert.Single(inDevelopment).EntityId);
        }

        [Fact]
        public async Task CreateAsync_WhenRequestingBoardIsSame_DeniesDespiteEdge()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var setup = await CreateConnectedRejectColumnsAsync(cancellationToken);
            var entityId = new EntityId(Guid.NewGuid());

            var operations = CreateRejectFanOutOperations(entityId, setup);

            // Act
            await Assert.ThrowsAsync<ColumnNotWritableException>(() =>
                _placementService.CreateAsync(
                    operations,
                    Guid.NewGuid(),
                    null,
                    new Actor("-1", "TestActor"),
                    cancellationToken,
                    setup.DevelopmentBoardId));

            // Assert
            Assert.Empty(await _repository.GetCurrentByColumnAsync(
                setup.TestingRejectId, setup.TestingBoardId));
            Assert.Empty(await _repository.GetCurrentByColumnAsync(
                setup.DevelopmentRejectedId, setup.DevelopmentBoardId));
        }

        [Fact]
        public async Task CreateAsync_NinetyConcurrentInsertionsInSameColumn_AllCompleteWithDistinctSortKeys()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Sort-key load test");
            var columnId = new ColumnId(Guid.NewGuid());

            await _columnService.CreateAsync(
                new CreateColumnDto(columnId, "Column 1", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("test-user", "User"),
                cancellationToken);

            var actor = new Actor("test-user", "User");
            var anchorEntityId = new EntityId(Guid.NewGuid());

            var anchorOperation = new PlacementOperationDto(
                new CreatePlacementDto(
                    [anchorEntityId], boardId, columnId, [], []),
                []);

            var anchorResult = await _placementService.CreateAsync(
                [anchorOperation],
                Guid.NewGuid(),
                null,
                actor,
                cancellationToken);

            Assert.Contains(anchorEntityId, anchorResult.SucceededEntityIds);

            const int requestCount = 90;

            var requests = Enumerable.Range(0, requestCount)
                .Select(_ =>
                {
                    var entityId = new EntityId(Guid.NewGuid());
                    var operation = new PlacementOperationDto(
                        new CreatePlacementDto(
                            [entityId], boardId, columnId, [anchorEntityId], []),
                        []);

                    return (EntityId: entityId, Operation: operation);
                })
                .ToArray();

            var startGate = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            var tasks = requests.Select(async request =>
            {
                await startGate.Task;

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                var result = await _placementService.CreateAsync(
                    [request.Operation],
                    Guid.NewGuid(),
                    null,
                    actor,
                    cancellationToken);

                stopwatch.Stop();

                return (request.EntityId, Result: result, stopwatch.Elapsed);
            }).ToArray();

            var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();
            startGate.SetResult(true);

            var results = await Task.WhenAll(tasks);

            totalStopwatch.Stop();

            Assert.All(results, item =>
            {
                Assert.Contains(item.EntityId, item.Result.SucceededEntityIds);
                Assert.Empty(item.Result.Failures);
            });

            var placements = (await _repository.GetCurrentByColumnAsync(columnId, boardId))
                .ToArray();

            Assert.Equal(requestCount + 1, placements.Length);
            Assert.Equal(
                placements.Length,
                placements.Select(placement => placement.SortKey).Distinct().Count());
        }

        [Fact]
        public async Task CreateAsync_OneThousandFiveHundredConcurrentInsertionsInSameColumnInOneMinute_AllCompleteWithDistinctSortKeys_WithOutput()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Sort-key load test");
            var columnId = new ColumnId(Guid.NewGuid());

            await _columnService.CreateAsync(
                new CreateColumnDto(columnId, "Column 1", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("test-user", "User"),
                cancellationToken);

            var actor = new Actor("test-user", "User");
            var anchorEntityId = new EntityId(Guid.NewGuid());

            var anchorOperation = new PlacementOperationDto(
                new CreatePlacementDto(
                    [anchorEntityId], boardId, columnId, [], []),
                []);

            var anchorResult = await _placementService.CreateAsync(
                [anchorOperation],
                Guid.NewGuid(),
                null,
                actor,
                cancellationToken);

            Assert.Contains(anchorEntityId, anchorResult.SucceededEntityIds);

            const int requestCount = 1500;
            var interval = TimeSpan.FromMilliseconds(60_000d / requestCount);

            var tasks = new List<Task>();
            var failures = new ConcurrentBag<string>();
            var elapsedTimes = new ConcurrentBag<TimeSpan>();
            var totalStopwatch = Stopwatch.StartNew();

            for (var i = 0; i < requestCount; i++)
            {
                await Task.Delay(interval, cancellationToken);

                var entityId = new EntityId(Guid.NewGuid());
                var operation = new PlacementOperationDto(
                    new CreatePlacementDto(
                        [entityId], boardId, columnId, [anchorEntityId], []),
                    []);

                tasks.Add(MeasureAsync(entityId, operation));
            }

            await Task.WhenAll(tasks);

            totalStopwatch.Stop();
            TestContext.Current.TestOutputHelper?.WriteLine(
                $"Längsta anrop: {elapsedTimes.Max().TotalMilliseconds:N0} ms");

            Assert.Empty(failures);
            Assert.Equal(requestCount, elapsedTimes.Count);

            var placements = (await _repository.GetCurrentByColumnAsync(columnId, boardId))
                .ToArray();

            Assert.Equal(requestCount + 1, placements.Length);
            Assert.Equal(
                placements.Length,
                placements.Select(placement => placement.SortKey).Distinct().Count());

            Console.WriteLine($"Längsta anrop: {elapsedTimes.Max().TotalMilliseconds:N0} ms");

            async Task MeasureAsync(EntityId entityId, PlacementOperationDto operation)
            {
                var stopwatch = Stopwatch.StartNew();

                var result = await _placementService.CreateAsync(
                    [operation],
                    Guid.NewGuid(),
                    null,
                    actor,
                    cancellationToken);

                stopwatch.Stop();
                elapsedTimes.Add(stopwatch.Elapsed);

                if (!result.SucceededEntityIds.Contains(entityId) || result.Failures.Count > 0)
                {
                    var error = string.Join("; ", result.Failures.Select(failure => failure.Error));
                    failures.Add($"Entity {entityId.Id}: {error}");
                }
            }
        }

        [Fact]
        public async Task CreateAsync_UnknownBoard_ReturnsFailure()
        {
            // Arrange
            var expectedBoardId = await _boardService.CreateAsync("Expected board");
            var columnId = new ColumnId(Guid.NewGuid());
            var cancellationToken = TestContext.Current.CancellationToken;

            await _columnService.CreateAsync(
                new CreateColumnDto(columnId, "Column 1", 1, expectedBoardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            var unknownBoardId = new BoardId(Guid.NewGuid());
            var entityId = new EntityId(Guid.NewGuid());

            var operation = new PlacementOperationDto(
                new CreatePlacementDto([entityId], unknownBoardId, columnId, [], []),
                []);

            // Act
            var result = await _placementService.CreateAsync(
                [operation],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                cancellationToken);

            // Assert
            Assert.Empty(result.SucceededEntityIds);

            var failure = Assert.Single(result.Failures);
            Assert.Contains("Board with id", failure.Error);
            Assert.Contains(unknownBoardId.Id.ToString(), failure.Error);
        }

        [Fact]
        public async Task CreateAsync_MultipleIds_StrictlyIncreasingSortKeys()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Testboard");
            var columnId = new ColumnId(Guid.NewGuid());

            var entityIds = Enumerable.Range(0, 10)
                .Select(_ => new EntityId(Guid.NewGuid()))
                .ToArray();

            var operation = new PlacementOperationDto(
                new CreatePlacementDto(entityIds, boardId, columnId, [], []),
                []);

            // Act
            var result = await _placementService.CreateAsync(
                [operation],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                cancellationToken);

            // Assert
            Assert.Equal(entityIds, result.SucceededEntityIds);

            var placements = (await _repository.GetCurrentByColumnAsync(columnId, boardId))
                .ToArray();

            Assert.Equal(entityIds.Length, placements.Length);
            Assert.Equal(entityIds, placements.Select(p => p.EntityId));

            for (var i = 1; i < placements.Length; i++)
            {
                Assert.True(
                    StringComparer.Ordinal.Compare(
                        placements[i - 1].SortKey,
                        placements[i].SortKey) < 0,
                    "Sort keys should be strictly increasing.");
            }
        }

        [Fact]
        public async Task CreateAsync_WritesOneOutboxRowPerPlacement()
        {
            // Arrange
            var boardId = await _boardService.CreateAsync("Testboard");
            var columnId = new ColumnId(Guid.NewGuid());
            var cancellationToken = TestContext.Current.CancellationToken;
            var entityIds = Enumerable.Range(0, 5)
            .Select(_ => new EntityId(Guid.NewGuid()))
            .ToArray();

            var operation = new PlacementOperationDto(
                new CreatePlacementDto(entityIds, boardId, columnId, [], []),
                []);

            var outboxRepository = new OutboxRepository(_database.DbFactory);

            await _columnService.CreateAsync(
                new CreateColumnDto(columnId, "Column 1", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            var outboxBefore = (await outboxRepository.GetUnprocessedAsync(cancellationToken))
                .Select(message => message.Id)
                .ToHashSet();

            // Act
            var result = await _placementService.CreateAsync(
                [operation],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                cancellationToken);

            // Assert
            Assert.Equal(entityIds, result.SucceededEntityIds);
            Assert.Empty(result.Failures);

            var outboxAfter = await outboxRepository.GetUnprocessedAsync(cancellationToken);
            var newMessages = outboxAfter
                .Where(message => !outboxBefore.Contains(message.Id))
                .ToArray();

            Assert.Equal(entityIds.Length, newMessages.Length);
        }

        [Fact]
        public async Task CreateAsync_CorrelationHeaders_EndUpInOutBox()
        {
            // Arrange
            var boardId = await _boardService.CreateAsync("Testboard");
            var columnId = new ColumnId(Guid.NewGuid());
            var cancellationToken = TestContext.Current.CancellationToken;
            var entityIds = Enumerable.Range(0, 3)
                .Select(_ => new EntityId(Guid.NewGuid()))
                .ToArray();
            var operation = new PlacementOperationDto(
                new CreatePlacementDto(entityIds, boardId, columnId, [], []),
                []);
            var outboxRepository = new OutboxRepository(_database.DbFactory);
            
            await _columnService.CreateAsync(
                new CreateColumnDto(columnId, "Column 1", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);
            
            var correlationId = Guid.NewGuid();
            var causationEventId = Guid.NewGuid();

            var outboxBefore = (await outboxRepository.GetUnprocessedAsync(cancellationToken))
                .Select(message => message.Id)
                .ToHashSet();

            // Act
            var result = await _placementService.CreateAsync(
                [operation],
                correlationId,
                causationEventId,
                new Actor("-1", "TestActor"),
                cancellationToken);

            // Assert
            Assert.Equal(entityIds, result.SucceededEntityIds);
            Assert.Empty(result.Failures);

            var outboxAfter = await outboxRepository.GetUnprocessedAsync(cancellationToken);
            var newMessages = outboxAfter
                .Where(message => !outboxBefore.Contains(message.Id))
                .ToArray();

            Assert.Equal(entityIds.Length, newMessages.Length);

            Assert.All(newMessages, message =>
            {
                using var document = JsonDocument.Parse(message.Content);
                var root = document.RootElement;

                Assert.Equal(
                    correlationId,
                    root.GetProperty("correlationId").GetGuid());

                Assert.Equal(
                    causationEventId,
                    root.GetProperty("causationEventId").GetGuid());
            });
        }

        [Fact]
        public async Task CreateAsync_SecondOpInvalid_FirstRemainsCommitted()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Testboard");
            var columnId = new ColumnId(Guid.NewGuid());
            var entityId1 = new EntityId(Guid.NewGuid());
            var entityId2 = new EntityId(Guid.NewGuid());

            await _columnService.CreateAsync(
                new CreateColumnDto(columnId, "Column 1", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            var operation1 = new PlacementOperationDto(
                new CreatePlacementDto([entityId1], boardId, columnId, [], []),
                []);
            var operation2 = new PlacementOperationDto(
                new CreatePlacementDto([entityId2], boardId, new ColumnId(Guid.NewGuid()), [], []),
                []);

            // Act
            var result = await _placementService.CreateAsync(
                [operation1, operation2],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                cancellationToken);

            // Assert
            Assert.Contains(entityId1, result.SucceededEntityIds);
            Assert.DoesNotContain(entityId2, result.SucceededEntityIds);

            var placement1 = await _repository.GetCurrentAsync([entityId1], boardId);

            Assert.Equal(columnId, Assert.Single(placement1).ColumnId);

            var placement2 = await _repository.GetCurrentAsync([entityId2], boardId);

            Assert.Empty(placement2);

            var failure = Assert.Single(result.Failures);
            Assert.Equal(entityId2, failure.EntityId);
        }

        private sealed record ConnectedRejectSetup(
            BoardId DevelopmentBoardId,
            BoardId TestingBoardId,
            ColumnId DevelopmentRejectedId,
            ColumnId TestingRejectId);

        private async Task<ConnectedRejectSetup> CreateConnectedRejectColumnsAsync(
            CancellationToken cancellationToken)
        {
            var developmentBoardId = await _boardService.CreateAsync("Development");
            var testingBoardId = await _boardService.CreateAsync("Testing");
            var developmentRejectedId = new ColumnId(Guid.NewGuid());
            var testingRejectId = new ColumnId(Guid.NewGuid());

            await _columnService.CreateAsync(
                new CreateColumnDto(developmentRejectedId, "Rejected", 1, developmentBoardId, false),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            await _columnService.CreateAsync(
                new CreateColumnDto(testingRejectId, "Reject", 1, testingBoardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            await _columnEdgeService.CreateAsync(
                new CreateColumnEdgeDto(developmentRejectedId, testingRejectId));

            return new ConnectedRejectSetup(
                developmentBoardId, testingBoardId, developmentRejectedId, testingRejectId);
        }

        private static PlacementOperationDto[] CreateRejectFanOutOperations(
            EntityId entityId,
            ConnectedRejectSetup setup) =>
                [
                    new PlacementOperationDto(
                        new CreatePlacementDto(
                            [entityId], setup.TestingBoardId, setup.TestingRejectId, [], []),
                        []),
                    new PlacementOperationDto(
                        new CreatePlacementDto(
                            [entityId], setup.DevelopmentBoardId, setup.DevelopmentRejectedId, [], []),
                        [])
                ];
    }
}
