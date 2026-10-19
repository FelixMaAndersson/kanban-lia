using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;

using kanban_lia.Infrastructure.Repositories.Boards;
using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Infrastructure.Repositories.Placements;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Exceptions;
using kanban_lia.Models.Domain.Placements;
using kanban_lia.Services.Boards;
using kanban_lia.Services.Columns;
using kanban_lia.Services.Columns.Exceptions;
using kanban_lia.Services.Columns.DTOs;
using kanban_lia.Services.Placements;
using kanban_lia.Mappings;
using kanban_lia.Infrastructure.Repositories.Outbox;
using kanban_lia.Services.Placements.DTOs;
using kanban_lia.Integration.Tests.Fixture;
using kanban_lia.Services.IntegrationEvents.Models;

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

        private readonly PlacementService _placementService = new(
            database.DbFactory,
            new BoardRepository(database.DbFactory),
            new ColumnRepository(database.DbFactory),
            new PlacementRepository(database.DbFactory),
            new OutboxRepository(database.DbFactory),
            _mapper);

        private readonly PlacementRepository _placementRepository = new(database.DbFactory);

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
            var placement1 = await _placementRepository.GetCurrentAsync(
                [operation1EntityId],
                boardId);

            var placement2 = await _placementRepository.GetCurrentAsync(
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
            var placements = await _placementRepository.GetCurrentByColumnAsync(columnId, boardId);
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
            var placement1 = await _placementRepository.GetCurrentAsync(
                [operation1EntityId],
                boardId);

            var placement2 = await _placementRepository.GetCurrentAsync(
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
            var placement = await _placementRepository.GetCurrentAsync(
                [new(Guid.NewGuid())],
                boardId);

            Assert.NotNull(placement);
        }

        [Fact]
        public async Task CreateAsync_WithBothBeforeAndAfter_ThrowsInvalidException()
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

            var operation = new PlacementOperationDto(
                new CreatePlacementDto(
                    [new(Guid.NewGuid())],
                    boardId,
                    columnId,
                    [new(Guid.NewGuid())],
                    [new(Guid.NewGuid())]),
                []);

            // Act and Assert
            await Assert.ThrowsAsync<InvalidDomainException>(
                () => _placementService.CreateAsync(
                    [operation],
                    Guid.NewGuid(),
                    null,
                    new Actor("-1", "TestActor"),
                    cancellationToken));
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
            var placement1 = await _placementRepository.GetCurrentAsync(
                [operation1EntityId],
                boardId);

            var placement2 = await _placementRepository.GetCurrentAsync(
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
            var boardService = CreateBoardService();
            var columnService = CreateColumnService();
            var placementService = CreatePlacementService();
            var repository = CreatePlacementRepository();

            var boardId = await boardService.CreateAsync("Anchor fallback test");
            var targetColumnId = new ColumnId(Guid.NewGuid());
            var otherColumnId = new ColumnId(Guid.NewGuid());

            await columnService.CreateAsync(
                new CreateColumnDto(targetColumnId, "Target", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                CancellationToken.None);

            await columnService.CreateAsync(
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

                await placementService.CreateAsync(
                    [operation],
                    Guid.NewGuid(),
                    null,
                    new Actor("-1", "TestActor"),
                    CancellationToken.None);
            }

            await CreatePlacementAsync(existingEntityId, targetColumnId);
            await CreatePlacementAsync(anchorEntityId, otherColumnId);

            var afterEntityIds = useAfterAnchor
                ? new[] { anchorEntityId }
                : Array.Empty<EntityId>();
            var beforeEntityIds = useAfterAnchor
                ? Array.Empty<EntityId>()
                : new[] { anchorEntityId };

            var operationWithMissingTargetAnchor = new PlacementOperationDto(
                new CreatePlacementDto(
                    [newEntityId],
                    boardId,
                    targetColumnId,
                    afterEntityIds,
                    beforeEntityIds),
                []);

            await placementService.CreateAsync(
                [operationWithMissingTargetAnchor],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                CancellationToken.None);

            var placements = (await repository.GetCurrentByColumnAsync(
                targetColumnId,
                boardId)).ToList();

            Assert.Equal(2, placements.Count);
            Assert.Equal(existingEntityId, placements[0].EntityId);
            Assert.Equal(newEntityId, placements[1].EntityId);
        }

        [Fact]
        public async Task CreateAsync_WhenColumnBelongsToDifferentBoard_ThrowsInvalidDomainException()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var requestedBoardId = await _boardService.CreateAsync("Requested board");
            var columnOwnerBoardId = await _boardService.CreateAsync("Column owner board");
            var columnId = new ColumnId(Guid.NewGuid());

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
                    [new EntityId(Guid.NewGuid())],
                    requestedBoardId,
                    columnId,
                    [],
                    []),
                []);

            // Act and Assert
            await Assert.ThrowsAsync<InvalidDomainException>(
                () => _placementService.CreateAsync(
                    [operation],
                    Guid.NewGuid(),
                    null,
                    new Actor("-1", "TestActor"),
                    cancellationToken));
        }

        [Fact]
        public async Task CreateAsync_WithNonExistingColumn_ThrowsException()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Testboard");
            var columnId = new ColumnId(Guid.NewGuid());

            var operation = new PlacementOperationDto(
                new CreatePlacementDto(
                    [new(Guid.NewGuid())],
                    boardId,
                    columnId,
                    [],
                    []),
                []);

            // Act and Assert
            await Assert.ThrowsAsync<ColumnNotFoundException>(
                () => _placementService.CreateAsync(
                    [operation],
                    Guid.NewGuid(),
                    null,
                    new Actor("-1", "TestActor"),
                    cancellationToken));
        }
    }
}
