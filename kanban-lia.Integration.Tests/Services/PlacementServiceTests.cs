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
    [Collection(DatabaseCollection.Name)]
    public class PlacementServiceTests(DatabaseFixture database) : IAsyncLifetime
    {
        private readonly DatabaseFixture _database = database;
        private readonly IMapper _mapper = new MapperConfiguration(
                cfg =>
                {
                    cfg.AddProfile<BoardProfile>();
                    cfg.AddProfile<ColumnProfile>();
                    cfg.AddProfile<PlacementProfile>();
                },
                NullLoggerFactory.Instance).CreateMapper();


        public async ValueTask InitializeAsync()
        {
            await _database.ResetAsync();
        }

        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }

        private PlacementService CreatePlacementService()
        {
            return new PlacementService(
                _database.DbFactory,
                new BoardRepository(_database.DbFactory),
                new ColumnRepository(_database.DbFactory),
                new PlacementRepository(_database.DbFactory),
                new OutboxRepository(_database.DbFactory),
                _mapper);
        }

        private PlacementRepository CreatePlacementRepository()
        {
            return new PlacementRepository(_database.DbFactory);
        }

        private ColumnService CreateColumnService()
        {
            return new ColumnService(
                new ColumnRepository(_database.DbFactory),
                new ColumnEdgeRepository(_database.DbFactory),
                _mapper);
        }

        private BoardService CreateBoardService()
        {
            return new BoardService(
                new BoardRepository(_database.DbFactory),
                _mapper);
        }

        [Fact]
        public async Task CreateAsync_WithAfterEntityId_UsesAfterLookup()
        {
            // Arrange
            var boardService = CreateBoardService();
            var columnService = CreateColumnService();
            var placementService = CreatePlacementService();
            var repository = CreatePlacementRepository();

            var boardId = await boardService.CreateAsync("Testboard");

            var columnId = new ColumnId(Guid.NewGuid());

            await columnService.CreateAsync(
                new CreateColumnDto(
                    columnId,
                    "Column 1",
                    1,
                    boardId,
                    true),
                    Guid.NewGuid(),
                    new Actor("-1", "TestActor"),
                    CancellationToken.None);

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

            await placementService.CreateAsync(
                [operation1],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                CancellationToken.None);

            // Act
            await placementService.CreateAsync(
                [operation2],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                CancellationToken.None);

            // Assert
            var placement1 = await repository.GetCurrentAsync(
                [operation1EntityId],
                boardId);

            var placement2 = await repository.GetCurrentAsync(
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
            var boardService = CreateBoardService();
            var columnService = CreateColumnService();
            var placementService = CreatePlacementService();
            var repository = CreatePlacementRepository();

            var boardId = await boardService.CreateAsync("Concurrent placement test");
            var columnId = new ColumnId(Guid.NewGuid());

            await columnService.CreateAsync(
                new CreateColumnDto(columnId, "Column 1", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("test-user", "User"),
                CancellationToken.None);

            var anchorEntityId = new EntityId(Guid.NewGuid());
            var anchorOperation = new PlacementOperationDto(
                new CreatePlacementDto(
                    [anchorEntityId], boardId, columnId, [], []),
                []);

            await placementService.CreateAsync(
                [anchorOperation], Guid.NewGuid(), null,
                new Actor("test-user", "User"), CancellationToken.None);

            var insertions = Enumerable.Range(0, 12).Select(_ =>
            {
                var entityId = new EntityId(Guid.NewGuid());
                var operation = new PlacementOperationDto(
                    new CreatePlacementDto(
                        [entityId], boardId, columnId, [anchorEntityId], []),
                    []);

                return placementService.CreateAsync(
                    [operation], Guid.NewGuid(), null,
                    new Actor("test-user", "User"), CancellationToken.None);
            });

            await Task.WhenAll(insertions);

            var placements = await repository.GetCurrentByColumnAsync(columnId, boardId);
            var sortKeys = placements.Select(placement => placement.SortKey).ToArray();

            Assert.Equal(13, sortKeys.Length);
            Assert.Equal(sortKeys.Length, sortKeys.Distinct().Count());
        }

        [Fact]
        public async Task CreateAsync_WithBeforeEntityId_UsesBeforeLookup()
        {
            // Arrange
            var boardService = CreateBoardService();
            var columnService = CreateColumnService();
            var placementService = CreatePlacementService();
            var repository = CreatePlacementRepository();

            var boardId = await boardService.CreateAsync("Testboard");

            var columnId = new ColumnId(Guid.NewGuid());

            await columnService.CreateAsync(
                new CreateColumnDto(
                    columnId,
                    "Column 1",
                    1,
                    boardId,
                    true),
                    Guid.NewGuid(),
                    new Actor("-1", "TestActor"),
                    CancellationToken.None);

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

            await placementService.CreateAsync(
                [operation1],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                CancellationToken.None);

            // Act
            await placementService.CreateAsync(
                [operation2],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                CancellationToken.None);

            // Assert
            var placement1 = await repository.GetCurrentAsync(
                [operation1EntityId],
                boardId);

            var placement2 = await repository.GetCurrentAsync(
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
            var boardService = CreateBoardService();
            var columnService = CreateColumnService();
            var placementService = CreatePlacementService();
            var repository = CreatePlacementRepository();

            var boardId = await boardService.CreateAsync("Testboard");

            var columnId = new ColumnId(Guid.NewGuid());

            await columnService.CreateAsync(
                new CreateColumnDto(
                    columnId,
                    "Column 1",
                    1,
                    boardId,
                    true),
                    Guid.NewGuid(),
                    new Actor("-1", "TestActor"),
                    CancellationToken.None);

            // Act
            var operation = new PlacementOperationDto(
                new CreatePlacementDto(
                    [new(Guid.NewGuid())],
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

            // Assert
            var placement = await repository.GetCurrentAsync(
                [new(Guid.NewGuid())],
                boardId);

            Assert.NotNull(placement);
        }

        [Fact]
        public async Task CreateAsync_WithBothBeforeAndAfter_ThrowsInvalidException()
        {
            // Arrange
            var boardService = CreateBoardService();
            var columnService = CreateColumnService();
            var placementService = CreatePlacementService();

            var boardId = await boardService.CreateAsync("Testboard");

            var columnId = new ColumnId(Guid.NewGuid());

            await columnService.CreateAsync(
                new CreateColumnDto(
                    columnId,
                    "Column 1",
                    1,
                    boardId,
                    true),
                    Guid.NewGuid(),
                    new Actor("-1", "TestActor"),
                    CancellationToken.None);

            // Act & Assert
            var operation = new PlacementOperationDto(
                new CreatePlacementDto(
                    [new(Guid.NewGuid())],
                    boardId,
                    columnId,
                    [new(Guid.NewGuid())],
                    [new(Guid.NewGuid())]),
                []);

            await Assert.ThrowsAsync<InvalidDomainException>(
                () => placementService.CreateAsync(
                [operation],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                CancellationToken.None));
        }

        [Fact]
        public async Task CreateAsync_WithoutBeforeOrAfter_WhenColumnHasPlacements_PlacesLast()
        {
            // Arrange
            var boardService = CreateBoardService();
            var columnService = CreateColumnService();
            var placementService = CreatePlacementService();
            var repository = CreatePlacementRepository();

            var boardId = await boardService.CreateAsync("Testboard");

            var columnId = new ColumnId(Guid.NewGuid());

            await columnService.CreateAsync(
                new CreateColumnDto(
                    columnId,
                    "Column 1",
                    1,
                    boardId,
                    true),
                    Guid.NewGuid(),
                    new Actor("-1", "TestActor"),
                    CancellationToken.None);

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

            await placementService.CreateAsync(
                [operation1],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                CancellationToken.None);

            // Act
            await placementService.CreateAsync(
                [operation2],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                CancellationToken.None);

            // Assert
            var placement1 = await repository.GetCurrentAsync(
                [operation1EntityId],
                boardId);

            var placement2 = await repository.GetCurrentAsync(
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
            var boardService = CreateBoardService();
            var columnService = CreateColumnService();
            var placementService = CreatePlacementService();

            var requestedBoardId = await boardService.CreateAsync("Requested board");
            var columnOwnerBoardId = await boardService.CreateAsync("Column owner board");
            var columnId = new ColumnId(Guid.NewGuid());

            await columnService.CreateAsync(
                new CreateColumnDto(
                    columnId,
                    "Column on another board",
                    1,
                    columnOwnerBoardId,
                    true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                CancellationToken.None);

            var operation = new PlacementOperationDto(
                new CreatePlacementDto(
                    [new EntityId(Guid.NewGuid())],
                    requestedBoardId,
                    columnId,
                    [],
                    []),
                []);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidDomainException>(
                () => placementService.CreateAsync(
                    [operation],
                    Guid.NewGuid(),
                    null,
                    new Actor("-1", "TestActor"),
                    CancellationToken.None));
        }

        [Fact]
        public async Task CreateAsync_WithNonExistingColumn_ThrowsException()
        {
            // Arrange
            var boardService = CreateBoardService();
            var placementService = CreatePlacementService();

            var boardId = await boardService.CreateAsync("Testboard");
            var columnId = new ColumnId(Guid.NewGuid());

            // Act & Assert
            var operation = new PlacementOperationDto(
                new CreatePlacementDto(
                    [new(Guid.NewGuid())],
                    boardId,
                    columnId,
                    [],
                    []),
                []);

            await Assert.ThrowsAsync<ColumnNotFoundException>(
                () => placementService.CreateAsync(
                [operation],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                CancellationToken.None));
        }
    }
}
