using AutoMapper;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

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

namespace kanban_lia.Integration.Tests.Services
{
    /// <summary>
    /// Class <c>ColumnServiceTests</c> is the test class for <c>ColumnService</c>
    /// </summary>
    /// <param name="database">Primary constructor for <c>DatabaseFixture</c></param>
    [Collection(DatabaseCollection.Name)]
    public sealed class ColumnServiceTests(DatabaseFixture database) : IAsyncLifetime
    {
        private readonly DatabaseFixture _database = database;

        private static readonly IMapper _mapper = new MapperConfiguration(
            cfg =>
            {
                cfg.AddProfile(new ColumnProfile());
                cfg.AddProfile(new ColumnEdgeProfile());
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
        public async Task CreateAsync_WithValidId_PersistsColumnThatCanBeRetrieved()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Test Board");
            var columnId = new ColumnId(Guid.NewGuid());

            // Act
            await _columnService.CreateAsync(
                new CreateColumnDto(columnId, "Test Column", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            var retrievedColumn = await _columnService.GetByIdAsync(columnId);

            // Assert
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
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Test Board");
            var columnId = new ColumnId(Guid.NewGuid());

            await _columnService.CreateAsync(
                new CreateColumnDto(columnId, "Test Column", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            // Act
            var exception = await Assert.ThrowsAsync<SqlException>(() => _columnService.CreateAsync(
                new CreateColumnDto(columnId, "Test Column2", 2, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken));

            // Assert
            Assert.Equal(2627, exception.Number);
            Assert.Single(await _columnService.GetByBoardIdAsync(boardId));
        }

        [Fact]
        public async Task CreateAsync_WithUnknownBoard_ThrowsSqlException547()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var columnId = new ColumnId(Guid.NewGuid());

            // Act
            var exception = await Assert.ThrowsAsync<SqlException>(() => _columnService.CreateAsync(
                new CreateColumnDto(columnId, "Test Column2", 2, new BoardId(Guid.NewGuid()), true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken));

            // Assert
            Assert.Equal(547, exception.Number);
        }

        [Fact]
        public async Task GetByBoardIdAsync_WithValidId_ReturnsAllColumnsForBoard()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Test Board");

            await _columnService.CreateAsync(
                new CreateColumnDto(new ColumnId(Guid.NewGuid()), "Column 1", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            await _columnService.CreateAsync(
                new CreateColumnDto(new ColumnId(Guid.NewGuid()), "Column 2", 2, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            // Act
            var columns = await _columnService.GetByBoardIdAsync(boardId);

            // Assert
            Assert.Equal(2, columns.Count());
            Assert.Contains(columns, c => c.Title == "Column 1");
            Assert.Contains(columns, c => c.Title == "Column 2");
        }

        [Fact]
        public async Task GetByBoardIdAsync_ReturnsOrderedByPosition()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Test Board");

            await _columnService.CreateAsync(
                new CreateColumnDto(new ColumnId(Guid.NewGuid()), "Column 2", 2, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            await _columnService.CreateAsync(
                new CreateColumnDto(new ColumnId(Guid.NewGuid()), "Column 1", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            await _columnService.CreateAsync(
                new CreateColumnDto(new ColumnId(Guid.NewGuid()), "Column 3", 3, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            // Act
            var columns = await _columnService.GetByBoardIdAsync(boardId);

            // Assert
            Assert.Equal(3, columns.Count());
            Assert.Equal(["Column 1", "Column 2", "Column 3"],
                columns.Select(column => column.Title));
        }

        [Fact]
        public async Task GetByBoardIdAsync_WithInvalidId_ThrowsBoardNotFoundException()
        {
            // Act and Assert
            await Assert.ThrowsAsync<BoardNotFoundException>(() =>
                _columnService.GetByBoardIdAsync(new BoardId(Guid.NewGuid())));
        }

        [Fact]
        public async Task GetById_WithValidId_ReturnsColumn()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Test Board");
            var columnId = new ColumnId(Guid.NewGuid());

            await _columnService.CreateAsync(
                new CreateColumnDto(columnId, "Column 1", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            // Act
            var returnedColumn = await _columnService.GetByIdAsync(columnId);

            // Assert
            Assert.NotNull(returnedColumn);
            Assert.Equal(columnId, returnedColumn.Id);
            Assert.Equal("Column 1", returnedColumn.Title);
        }

        [Fact]
        public async Task RenameAsync_WithValidId_UpdatesColumnTitle()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Test Board");
            var columnId = new ColumnId(Guid.NewGuid());

            await _columnService.CreateAsync(
                new CreateColumnDto(columnId, "Old Title", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            // Act
            var renamed = await _columnService.RenameAsync(new RenameColumnDto(columnId, "New Title"));
            var updatedColumn = await _columnService.GetByIdAsync(columnId);

            // Assert
            Assert.True(renamed);
            Assert.NotNull(updatedColumn);
            Assert.Equal("New Title", updatedColumn.Title);
        }

        [Fact]
        public async Task RenameAsync_NonExistingColumn_ThrowsColumnNotFoundException()
        {
            // Act and Assert
            await Assert.ThrowsAsync<ColumnNotFoundException>(() =>
                _columnService.RenameAsync(new RenameColumnDto(new ColumnId(Guid.NewGuid()), "New Title")));
        }

        [Fact]
        public async Task SetRequestWritableAsync_ExistingColumn_ReturnsTrueAndPersists()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Test Board");
            var columnId = new ColumnId(Guid.NewGuid());

            await _columnService.CreateAsync(
                new CreateColumnDto(columnId, "Column 1", 1, boardId, false),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            var returnedColumn = await _columnService.GetByIdAsync(columnId);

            // Act
            var changed = await _columnService.SetRequestWritableAsync(returnedColumn!.Id, true);
            var updated = await _columnService.GetByIdAsync(columnId);

            // Assert
            Assert.True(changed);
            Assert.True(updated!.RequestWritable);
        }

        [Fact]
        public async Task SetRequestWritableAsync_NonExisting_ThrowsColumnNotFound()
        {
            // Arrange
            var columnId = new ColumnId(Guid.NewGuid());

            // Act and Assert
            await Assert.ThrowsAsync<ColumnNotFoundException>(() =>
                _columnService.SetRequestWritableAsync(columnId, true));
        }

        [Fact]
        public async Task DeleteAsync_DeletesColumn()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Test Board");
            var columnId = new ColumnId(Guid.NewGuid());

            await _columnService.CreateAsync(
                new CreateColumnDto(columnId, "Old Title", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            // Act
            var deleted = await _columnService.DeleteAsync(columnId);

            // Assert
            Assert.True(deleted);
            await Assert.ThrowsAsync<ColumnNotFoundException>(() => _columnService.GetByIdAsync(columnId));
        }

        [Fact]
        public async Task DeleteAsync_RemovesConnectedEdges()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Test Board");
            var columnA = new ColumnId(Guid.NewGuid());
            var columnB = new ColumnId(Guid.NewGuid());
            var columnC = new ColumnId(Guid.NewGuid());

            await _columnService.CreateAsync(
                new CreateColumnDto(columnA, "Column A", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            await _columnService.CreateAsync(
                new CreateColumnDto(columnB, "Column B", 2, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            await _columnService.CreateAsync(
                new CreateColumnDto(columnC, "Column C", 3, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            await _columnEdgeService.CreateAsync(new CreateColumnEdgeDto(columnA, columnB));
            await _columnEdgeService.CreateAsync(new CreateColumnEdgeDto(columnB, columnC));

            var edgesTouchingA = (await _columnEdgeService.GetByFromColumnIdAsync(columnA))
                .Concat(await _columnEdgeService.GetByToColumnIdAsync(columnA));
            Assert.NotEmpty(edgesTouchingA);

            // Act
            var deleted = await _columnService.DeleteAsync(columnA);

            // Assert
            Assert.True(deleted);
            Assert.Empty(await _columnEdgeService.GetByFromColumnIdAsync(columnA));
            Assert.Empty(await _columnEdgeService.GetByToColumnIdAsync(columnA));

            var remaining = await _columnEdgeService.GetByBoardIdAsync(boardId);
            var item = Assert.Single(remaining);
            Assert.True(
                item.FromColumnId == columnB && item.ToColumnId == columnC ||
                item.FromColumnId == columnC && item.ToColumnId == columnB);
        }

        [Fact]
        public async Task DeleteAsync_RemovesPlacementsForColumn()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = await _boardService.CreateAsync("Test Board");
            var columnId = new ColumnId(Guid.NewGuid());

            await _columnService.CreateAsync(
                new CreateColumnDto(columnId, "Column 1", 1, boardId, true),
                Guid.NewGuid(),
                new Actor("-1", "TestActor"),
                cancellationToken);

            var entityId = new EntityId(Guid.NewGuid());

            await _placementService.CreateAsync(
                [new PlacementOperationDto(
                    new CreatePlacementDto([entityId], boardId, columnId, [], []),
                    [])],
                Guid.NewGuid(),
                null,
                new Actor("-1", "TestActor"),
                cancellationToken);

            Assert.NotEmpty(await _placementService.GetCurrentByColumnAsync(columnId, boardId));

            // Act
            await _columnService.DeleteAsync(columnId);

            // Assert
            Assert.Empty(await _placementService.GetCurrentByColumnAsync(columnId, boardId));
        }
    }
}
