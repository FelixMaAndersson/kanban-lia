using AutoMapper;
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
using kanban_lia.Services.Boards.Exceptions;
using kanban_lia.Services.Columns;
using kanban_lia.Services.Columns.DTOs;
using kanban_lia.Services.Columns.Exceptions;
using kanban_lia.Services.IntegrationEvents.Models;
using kanban_lia.Services.Placements;
using kanban_lia.Services.Placements.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace kanban_lia.Integration.Tests.Services
{
    [Collection(DatabaseCollection.Name)]
    public class ColumnServiceTests(DatabaseFixture database)
    {
        private readonly DatabaseFixture _database = database;
        private static readonly IMapper _mapper = new MapperConfiguration(
            cfg =>
            {
                cfg.AddProfile(new ColumnProfile());
                cfg.AddProfile(new ColumnEdgeProfile());
                cfg.AddProfile(new PlacementProfile());
            },
            NullLoggerFactory.Instance
        ).CreateMapper();


        private ColumnService CreateColumnService()
        {
            return new ColumnService(
                new ColumnRepository(_database.DbFactory),
                new BoardRepository(_database.DbFactory),
                new ColumnEdgeRepository(_database.DbFactory),
                _mapper);
        }

        private BoardService CreateBoardService()
        {
            return new BoardService(new BoardRepository(_database.DbFactory), _mapper);
        }

        private ColumnEdgeService CreateColumnEdgeService()
        {
            return new ColumnEdgeService(
                new ColumnEdgeRepository(_database.DbFactory),
                _mapper);
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

        [Fact]
        public async Task CreateAsync_WithValidId_PersistsColumnThatCanBeRetrieved()
        {
            var cancellationToken = TestContext.Current.CancellationToken;

            var columnService = CreateColumnService();
            var boardService = CreateBoardService();

            var boardId = await boardService.CreateAsync("Test Board");
            var columnId = new ColumnId(Guid.NewGuid());

            await columnService.CreateAsync(
                new CreateColumnDto(columnId, "Test Column", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            var retrievedColumn = await columnService.GetByIdAsync(columnId);

            Assert.NotNull(retrievedColumn);
            Assert.Equal("Test Column", retrievedColumn.Title);
            Assert.Equal(columnId, retrievedColumn.Id);
            Assert.Equal(boardId, retrievedColumn.BoardId);
            Assert.Equal(1, retrievedColumn.Position);
            Assert.True(retrievedColumn.RequestWritable);
        }

        [Fact]
        public async Task CreateAsync_WithDuplicateId_ThrowsSqlException2627()
        {
            var cancellationToken = TestContext.Current.CancellationToken;

            var columnService = CreateColumnService();
            var boardService = CreateBoardService();

            var boardId = await boardService.CreateAsync("Test Board");
            var columnId = new ColumnId(Guid.NewGuid());

            await columnService.CreateAsync(
                new CreateColumnDto(columnId, "Test Column", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            var exception = await Assert.ThrowsAsync<SqlException>(() => columnService.CreateAsync(
                 new CreateColumnDto(columnId, "Test Column2", 2, boardId, true),
                 Guid.NewGuid(),
                 new Actor("-1", "TestActor"),
                 cancellationToken));

            Assert.Equal(2627, exception.Number);

            var columns = await columnService.GetByBoardIdAsync(boardId);
            Assert.Single(columns);
        }

        [Fact]
        public async Task CreateAsync_WithUnknownBoard_ThrowsSqlException547()
        {
            var cancellationToken = TestContext.Current.CancellationToken;

            var columnService = CreateColumnService();

            var columnId = new ColumnId(Guid.NewGuid());

            var exception = await Assert.ThrowsAsync<SqlException>(() => columnService.CreateAsync(
                 new CreateColumnDto(columnId, "Test Column2", 2, new BoardId(Guid.NewGuid()), true),
                 Guid.NewGuid(),
                 new Actor("-1", "TestActor"),
                 cancellationToken));

            Assert.Equal(547, exception.Number);
        }

        [Fact]
        public async Task GetByBoardIdAsync_WithValidId_ReturnsAllColumnsForBoard()
        {
            var cancellationToken = TestContext.Current.CancellationToken;

            var columnService = CreateColumnService();
            var boardService = CreateBoardService();

            var boardId = await boardService.CreateAsync("Test Board");

            await columnService.CreateAsync(
                new CreateColumnDto(new ColumnId(Guid.NewGuid()), "Column 1", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            await columnService.CreateAsync(
                new CreateColumnDto(new ColumnId(Guid.NewGuid()), "Column 2", 2, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            var columns = await columnService.GetByBoardIdAsync(boardId);

            Assert.Equal(2, columns.Count());
            Assert.Contains(columns, c => c.Title == "Column 1");
            Assert.Contains(columns, c => c.Title == "Column 2");
        }

        [Fact]
        public async Task GetByBoardIdAsync_ReturnsOrderedByPosition()
        {
            var cancellationToken = TestContext.Current.CancellationToken;

            var columnService = CreateColumnService();
            var boardService = CreateBoardService();

            var boardId = await boardService.CreateAsync("Test Board");

            await columnService.CreateAsync(
                new CreateColumnDto(new ColumnId(Guid.NewGuid()), "Column 2", 2, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            await columnService.CreateAsync(
                new CreateColumnDto(new ColumnId(Guid.NewGuid()), "Column 1", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            await columnService.CreateAsync(
                new CreateColumnDto(new ColumnId(Guid.NewGuid()), "Column 3", 3, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            var columns = await columnService.GetByBoardIdAsync(boardId);

            Assert.Equal(3, columns.Count());
            Assert.Equal(["Column 1", "Column 2", "Column 3"],
                columns.Select(column => column.Title));
        }

        [Fact]
        public async Task GetByBoardIdAsync_WithInvalidId_ThrowsBoardNotFoundException()
        {

            var columnService = CreateColumnService();

            await Assert.ThrowsAsync<BoardNotFoundException>(() =>
                columnService.GetByBoardIdAsync(new BoardId(Guid.NewGuid())));
        }

        [Fact]
        public async Task GetById_WithValidId_ReturnsColumn()
        {
            var cancellationToken = TestContext.Current.CancellationToken;

            var columnService = CreateColumnService();
            var boardService = CreateBoardService();

            var boardId = await boardService.CreateAsync("Test Board");
            var columnId = new ColumnId(Guid.NewGuid());

            await columnService.CreateAsync(
                new CreateColumnDto(columnId, "Column 1", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            var returnedColumn = await columnService.GetByIdAsync(columnId);

            Assert.NotNull(returnedColumn);
            Assert.Equal(columnId, returnedColumn.Id);
            Assert.Equal("Test Board", returnedColumn.Title);
        }

        [Fact]
        public async Task RenameAsync_WithValidId_UpdatesColumnTitle()
        {
            var cancellationToken = TestContext.Current.CancellationToken;

            var columnService = CreateColumnService();
            var boardService = CreateBoardService();

            var boardId = await boardService.CreateAsync("Test Board");
            var columnId = new ColumnId(Guid.NewGuid());

            await columnService.CreateAsync(
                new CreateColumnDto(columnId, "Old Title", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            var renamed = await columnService.RenameAsync(new RenameColumnDto(columnId, "New Title"));

            var updatedColumn = await columnService.GetByIdAsync(columnId);

            Assert.True(renamed);
            Assert.NotNull(updatedColumn);
            Assert.Equal("New Title", updatedColumn.Title);
        }

        [Fact]
        public async Task RenameAsync_NonExistingColumn_ThrowsColumnNotFoundException()
        {

            var columnService = CreateColumnService();

            await Assert.ThrowsAsync<ColumnNotFoundException>(() =>
                columnService.RenameAsync(new RenameColumnDto(new ColumnId(Guid.NewGuid()), "New Title")));
        }

        [Fact]
        public async Task SetRequestWritableAsync_ExistingColumn_ReturnsTrueAndPersists()
        {
            var cancellationToken = TestContext.Current.CancellationToken;

            var columnService = CreateColumnService();
            var boardService = CreateBoardService();

            var boardId = await boardService.CreateAsync("Test Board");
            var columnId = new ColumnId(Guid.NewGuid());

            await columnService.CreateAsync(
                new CreateColumnDto(columnId, "Column 1", 1, boardId, false),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            var returnedColumn = await columnService.GetByIdAsync(columnId);

            var changed = await columnService.SetRequestWritableAsync(returnedColumn!.Id, true);

            Assert.True(changed);

            var updated = await columnService.GetByIdAsync(columnId);

            Assert.True(updated!.RequestWritable);
        }

        [Fact]
        public async Task SetRequestWritableAsync_NonExisting_ThrowsColumnNotFound()
        {

            var columnService = CreateColumnService();
            var columnId = new ColumnId(Guid.NewGuid());

            await Assert.ThrowsAsync<ColumnNotFoundException>(() =>
                columnService.SetRequestWritableAsync(columnId, true));
        }

        [Fact]
        public async Task DeleteAsync_DeletesColumn()
        {
            var cancellationToken = TestContext.Current.CancellationToken;

            var columnService = CreateColumnService();
            var boardService = CreateBoardService();

            var boardId = await boardService.CreateAsync("Test Board");
            var columnId = new ColumnId(Guid.NewGuid());

            await columnService.CreateAsync(
                new CreateColumnDto(columnId, "Old Title", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            var deleted = await columnService.DeleteAsync(columnId);

            Assert.True(deleted);
            await Assert.ThrowsAsync<ColumnNotFoundException>(() => columnService.GetByIdAsync(columnId));
        }

        [Fact]
        public async Task DeleteAsync_RemovesConnectedEdges()
        {
            var cancellationToken = TestContext.Current.CancellationToken;

            var columnService = CreateColumnService();
            var boardService = CreateBoardService();
            var edgeService = CreateColumnEdgeService();

            var boardId = await boardService.CreateAsync("Test Board");
            var columnA = new ColumnId(Guid.NewGuid());
            var columnB = new ColumnId(Guid.NewGuid());
            var columnC = new ColumnId(Guid.NewGuid());

            await columnService.CreateAsync(
                new CreateColumnDto(columnA, "Column A", 1, boardId, true),
                Guid.NewGuid(), new Actor("-1", "TestActor"), cancellationToken);
            await columnService.CreateAsync(
                new CreateColumnDto(columnB, "Column B", 2, boardId, true),
                Guid.NewGuid(), new Actor("-1", "TestActor"), cancellationToken);
            await columnService.CreateAsync(
                new CreateColumnDto(columnC, "Column C", 3, boardId, true),
                Guid.NewGuid(), new Actor("-1", "TestActor"), cancellationToken);

            await edgeService.CreateAsync(new CreateColumnEdgeDto(columnA, columnB));
            await edgeService.CreateAsync(new CreateColumnEdgeDto(columnB, columnA));
            await edgeService.CreateAsync(new CreateColumnEdgeDto(columnB, columnC));

            Assert.NotEmpty(await edgeService.GetByFromColumnIdAsync(columnA));

            var deleted = await columnService.DeleteAsync(columnA);
            Assert.True(deleted);

            Assert.Empty(await edgeService.GetByFromColumnIdAsync(columnA));
            Assert.Empty(await edgeService.GetByToColumnIdAsync(columnA));

            var remaining = await edgeService.GetByFromColumnIdAsync(columnB);
            var item = Assert.Single(remaining);
            Assert.Equal(columnC, item.ToColumnId);
        }

        [Fact]
        public async Task DeleteAsync_RemovesPlacementsForColumn()
        {
            var cancellationToken = TestContext.Current.CancellationToken;

            var columnService = CreateColumnService();
            var boardService = CreateBoardService();
            var placementService = CreatePlacementService();

            var boardId = await boardService.CreateAsync("Test Board");
            var columnId = new ColumnId(Guid.NewGuid());

            await columnService.CreateAsync(
                new CreateColumnDto(columnId, "Column 1", 1, boardId, true),
                Guid.NewGuid(), new Actor("-1", "TestActor"), cancellationToken);

            var entityId = new EntityId(Guid.NewGuid());
            await placementService.CreateAsync(
                [new PlacementOperationDto(
            new CreatePlacementDto([entityId], boardId, columnId, [], []),
            [])],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                cancellationToken);

            Assert.NotEmpty(await placementService.GetCurrentByColumnAsync(columnId, boardId));

            await columnService.DeleteAsync(columnId);

            Assert.Empty(await placementService.GetCurrentByColumnAsync(columnId, boardId));
        }
    }
}
