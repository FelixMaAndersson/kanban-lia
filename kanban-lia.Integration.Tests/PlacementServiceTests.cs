using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.SignalR;

using kanban_lia.Infrastructure.Repositories.Boards;
using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Infrastructure.Repositories.Placements;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Exceptions;
using kanban_lia.Models.Domain.Placements;
using kanban_lia.Services.Boards;
using kanban_lia.Services.Columns;
using kanban_lia.Services.Columns.Exceptions;
using kanban_lia.Services.Placements;
using kanban_lia.Mappings;
using kanban_lia.Infrastructure.Repositories.Outbox;
using kanban_lia.Services.IntegrationEvents.Models;
using kanban_lia.Services.Placements.DTOs;

namespace kanban_lia.Integration.Tests
{
    public class PlacementServiceTests : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _db;
        private readonly IMapper _mapper;

        public PlacementServiceTests(DatabaseFixture db)
        {
            _db = db;

            var config = new MapperConfiguration(
                cfg =>
                {
                    cfg.AddProfile<BoardProfile>();
                    cfg.AddProfile<ColumnProfile>();
                    cfg.AddProfile<PlacementProfile>();
                },
                NullLoggerFactory.Instance);

            _mapper = config.CreateMapper();
        }

        private PlacementService CreatePlacementService()
        {
            return new PlacementService(
                _db.DbFactory,
                new BoardRepository(_db.DbFactory),
                new ColumnRepository(_db.DbFactory),
                new PlacementRepository(_db.DbFactory),
                new OutboxRepository(_db.DbFactory),
                _mapper);
        }

        private PlacementRepository CreatePlacementRepository()
        {
            return new PlacementRepository(_db.DbFactory);
        }

        private ColumnService CreateColumnService()
        {
            return new ColumnService(
                new ColumnRepository(_db.DbFactory),
                new ColumnEdgeRepository(_db.DbFactory),
                _mapper);
        }

        private BoardService CreateBoardService()
        {
            return new BoardService(
                new BoardRepository(_db.DbFactory),
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
                new Services.Columns.DTOs.CreateColumnDto(
                    columnId,
                    "Column 1",
                    1,
                    boardId,
                    true),
                    Guid.NewGuid(), null, CancellationToken.None);

            var operation1 = new PlacementOperationDto(
                new CreatePlacementDto(
                    [new(Guid.NewGuid())],
                    boardId,
                    columnId,
                    null,
                    null),
                null);
            
            var operation2 = new PlacementOperationDto(
                new CreatePlacementDto(
                    [new(Guid.NewGuid())],
                    boardId,
                    columnId,
                    null,
                    null),
                null);

            await placementService.CreateAsync(
                [operation1],
                Guid.NewGuid(),
                null,
                null,
                CancellationToken.None);

            // Act
            await placementService.CreateAsync(
                [operation2],
                Guid.NewGuid(),
                null,
                null,
                CancellationToken.None);

            // Assert
            var placement1 = await repository.GetCurrentAsync(
                [new(Guid.NewGuid())],
                boardId);

            var placement2 = await repository.GetCurrentAsync(
                [new(Guid.NewGuid())],
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
                    boardId,
                    true),
                    Guid.NewGuid(), null, CancellationToken.None);

            var operation1 = new PlacementOperationDto(
                new CreatePlacementDto(
                    [new(Guid.NewGuid())],
                    boardId,
                    columnId,
                    null,
                    null),
                null);

            var operation2 = new PlacementOperationDto(
                new CreatePlacementDto(
                    [new(Guid.NewGuid())],
                    boardId,
                    columnId,
                    null,
                    null),
                null);

            await placementService.CreateAsync(
                [operation1],
                Guid.NewGuid(),
                null,
                null,
                CancellationToken.None);

            // Act
            await placementService.CreateAsync(
                [operation2],
                Guid.NewGuid(),
                null,
                null,
                CancellationToken.None);

            // Assert
            var placement1 = await repository.GetCurrentAsync(
                [new(Guid.NewGuid())],
                boardId);

            var placement2 = await repository.GetCurrentAsync(
                [new(Guid.NewGuid())],
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
                new Services.Columns.DTOs.CreateColumnDto(
                    columnId,
                    "Column 1",
                    1,
                    boardId,
                    true),
                    Guid.NewGuid(), null, CancellationToken.None);

            // Act
            var operation = new PlacementOperationDto(
                new CreatePlacementDto(
                    [new(Guid.NewGuid())],
                    boardId,
                    columnId,
                    null,
                    null),
                null);

            await placementService.CreateAsync(
                [operation],
                Guid.NewGuid(),
                null,
                null,
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
                new Services.Columns.DTOs.CreateColumnDto(
                    columnId,
                    "Column 1",
                    1,
                    boardId,
                    true),
                    Guid.NewGuid(), null, CancellationToken.None);

            var dto = new CreatePlacementDto(
                [new(Guid.NewGuid())],
                boardId,
                columnId,
                null,
                null);

            // Act & Assert
            var operation = new PlacementOperationDto(
                new CreatePlacementDto(
                    [new(Guid.NewGuid())],
                    boardId,
                    columnId,
                    null,
                    null),
                null);

            await Assert.ThrowsAsync<InvalidDomainException>(
                () => placementService.CreateAsync(
                [operation],
                Guid.NewGuid(),
                null,
                null,
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
                new Services.Columns.DTOs.CreateColumnDto(
                    columnId,
                    "Column 1",
                    1,
                    boardId,
                    true),
                    Guid.NewGuid(), null, CancellationToken.None);

            var entityId1 = Guid.NewGuid();
            var entityId2 = Guid.NewGuid();

            var operation1 = new PlacementOperationDto(
                new CreatePlacementDto(
                    [new(Guid.NewGuid())],
                    boardId,
                    columnId,
                    null,
                    null),
                null);

            var operation2 = new PlacementOperationDto(
                new CreatePlacementDto(
                    [new(Guid.NewGuid())],
                    boardId,
                    columnId,
                    null,
                    null),
                null);

            await placementService.CreateAsync(
                [operation1],
                Guid.NewGuid(),
                null,
                null,
                CancellationToken.None);

            // Act
            await placementService.CreateAsync(
                [operation2],
                Guid.NewGuid(),
                null,
                null,
                CancellationToken.None);

            // Assert
            var placement1 = await repository.GetCurrentAsync(
                [new(Guid.NewGuid())],
                boardId);

            var placement2 = await repository.GetCurrentAsync(
                [new(Guid.NewGuid())],
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

        [Fact]
        public async Task CreateAsync_WithNonExistingColumn_ThrowsException()
        {
            // Arrange
            var boardService = CreateBoardService();
            var placementService = CreatePlacementService();
            var columnService = CreateColumnService();

            var boardId = await boardService.CreateAsync("Testboard");
            var columnId = new ColumnId(Guid.NewGuid());

            var dto = new CreatePlacementDto(
                [new(Guid.NewGuid())],
                boardId,
                columnId,
                null,
                null);

            // Act & Assert
            var operation = new PlacementOperationDto(
                new CreatePlacementDto(
                    [new(Guid.NewGuid())],
                    boardId,
                    columnId,
                    null,
                    null),
                null);

            await Assert.ThrowsAsync<ColumnNotFoundException>(
                () => placementService.CreateAsync(
                [operation],
                Guid.NewGuid(),
                null,
                null,
                CancellationToken.None));
        }
    }
}