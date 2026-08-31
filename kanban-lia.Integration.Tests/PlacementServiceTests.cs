using kanban_lia.Infrastructure.Repositories.Boards;
using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Infrastructure.Repositories.Placements;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Exceptions;
using kanban_lia.Models.Domain.Placements;
using kanban_lia.Services.Boards;
using kanban_lia.Services.Columns;
using kanban_lia.Services.Placements.Exceptions;
using kanban_lia.Services.Placements;

namespace kanban_lia.Integration.Tests
{
    public class PlacementServiceTests(DatabaseFixture db) : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _db = db;

        private PlacementService CreatePlacementService()
        {
            return new PlacementService(new PlacementRepository(_db.DbFactory), new ColumnRepository(_db.DbFactory));
        }

        private PlacementRepository CreatePlacementRepository()
        {
            return new PlacementRepository(_db.DbFactory);
        }

        private ColumnService CreateColumnService()
        {
            return new ColumnService(new ColumnRepository(_db.DbFactory));
        }

        private BoardService CreateBoardService()
        {
            return new BoardService(new BoardRepository(_db.DbFactory));
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
                new Services.Columns.DTOs.CreateColumnDto(
                    columnId,
                    "Column 1",
                    1,
                    boardId));

            var entityId1 = Guid.NewGuid();
            var entityId2 = Guid.NewGuid();

            await placementService.CreateAsync(
                new Services.Placements.DTOs.CreatePlacementDto(
                    entityId1,
                    columnId,
                    null,
                    null));

            // Act
            await placementService.CreateAsync(
                new Services.Placements.DTOs.CreatePlacementDto(
                    entityId2,
                    columnId,
                    entityId1,
                    null));

            // Assert
            var placement1 = await repository.GetCurrentAsync(
                new EntityId(entityId1),
                boardId);

            var placement2 = await repository.GetCurrentAsync(
                new EntityId(entityId2),
                boardId);

            Assert.NotNull(placement1);
            Assert.NotNull(placement2);

            Assert.True(
                string.CompareOrdinal(
                    placement1.SortKey,
                    placement2.SortKey) < 0,
                $"Expected entity2 to be after entity1. " +
                $"entity1 SortKey: '{placement1.SortKey}', " +
                $"entity2 SortKey: '{placement2.SortKey}'");
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
                new Services.Columns.DTOs.CreateColumnDto(
                    columnId,
                    "Column 1",
                    1,
                    boardId));
            var entityId1 = Guid.NewGuid();
            var entityId2 = Guid.NewGuid();
            await placementService.CreateAsync(
                new Services.Placements.DTOs.CreatePlacementDto(
                    entityId1,
                    columnId,
                    null,
                    null));

            // Act
            await placementService.CreateAsync(
                new Services.Placements.DTOs.CreatePlacementDto(
                    entityId2,
                    columnId,
                    null,
                    entityId1));

            // Assert
            var placement1 = await repository.GetCurrentAsync(
                new EntityId(entityId1),
                boardId);

            var placement2 = await repository.GetCurrentAsync(
                new EntityId(entityId2),
                boardId);

            Assert.NotNull(placement1);
            Assert.NotNull(placement2);

            Assert.True(
                string.CompareOrdinal(
                    placement1.SortKey,
                    placement2.SortKey) > 0,
                $"Expected entity2 to be before entity1. " +
                $"entity1 SortKey: '{placement1.SortKey}', " +
                $"entity2 SortKey: '{placement2.SortKey}'");
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
                new Services.Columns.DTOs.CreateColumnDto(
                    columnId,
                    "Column 1",
                    1,
                    boardId));
            var entityId1 = Guid.NewGuid();

            // Act
            await placementService.CreateAsync(
                new Services.Placements.DTOs.CreatePlacementDto(
                    entityId1,
                    columnId,
                    null,
                    null));

            // Assert
            var placement1 = await repository.GetCurrentAsync(
                new EntityId(entityId1),
                boardId);
            Assert.NotNull(placement1);
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
                new Services.Columns.DTOs.CreateColumnDto(
                    columnId,
                    "Column 1",
                    1,
                    boardId));

            var dto = new Services.Placements.DTOs.CreatePlacementDto(
                Guid.NewGuid(),
                columnId,
                Guid.NewGuid(),
                Guid.NewGuid());

            // Act & Assert
            await Assert.ThrowsAsync<InvalidDomainException>(
                () => placementService.CreateAsync(dto));
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
                new Services.Columns.DTOs.CreateColumnDto(
                    columnId,
                    "Column 1",
                    1,
                    boardId));

            var entityId1 = Guid.NewGuid();
            var entityId2 = Guid.NewGuid();

            await placementService.CreateAsync(
                new Services.Placements.DTOs.CreatePlacementDto(
                    entityId1,
                    columnId,
                    null,
                    null));

            // Act
            await placementService.CreateAsync(
                new Services.Placements.DTOs.CreatePlacementDto(
                    entityId2,
                    columnId,
                    null,
                    null));

            // Assert
            var placement1 = await repository.GetCurrentAsync(
                new EntityId(entityId1),
                boardId);

            var placement2 = await repository.GetCurrentAsync(
                new EntityId(entityId2),
                boardId);

            Assert.NotNull(placement1);
            Assert.NotNull(placement2);

            Assert.NotEqual(
                placement1.SortKey,
                placement2.SortKey);

            Assert.True(
                string.CompareOrdinal(
                    placement1.SortKey,
                    placement2.SortKey) < 0,
                $"Expected entity2 to be placed after entity1. " +
                $"entity1 SortKey: '{placement1.SortKey}', " +
                $"entity2 SortKey: '{placement2.SortKey}'");
        }

        [Fact]
        public async Task CreateAsync_WithNonExistingColumn_ThrowsException()
        {
            // Arrange
            var boardService = CreateBoardService();
            var placementService = CreatePlacementService();
            var columnService = CreateColumnService();

            var boardId = await boardService.CreateAsync("Testboard");
            var columnId = new ColumnId(Guid.NewGuid());

            var dto = new Services.Placements.DTOs.CreatePlacementDto(
                Guid.NewGuid(),
                columnId,
                null,
                null);

            // Act & Assert
            await Assert.ThrowsAsync<ColumnNotFoundException>(
                () => placementService.CreateAsync(dto));
        }
    }
}