using AutoMapper;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

using kanban_lia.Infrastructure.Repositories.Boards;
using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Integration.Tests.Fixture;
using kanban_lia.Mappings;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Exceptions;
using kanban_lia.Services.Boards;
using kanban_lia.Services.Columns;
using kanban_lia.Services.Columns.DTOs;
using kanban_lia.Services.Columns.Exceptions;
using Actor = kanban_lia.Services.IntegrationEvents.Models.Actor;

namespace kanban_lia.Integration.Tests.Services
{
    [Collection(DatabaseCollection.Name)]
    public sealed class ColumnEdgeServiceTests(DatabaseFixture database) : IAsyncLifetime
    {
        private readonly DatabaseFixture _database = database;

        private static readonly IMapper _mapper = new MapperConfiguration(
            cfg =>
            {
                cfg.AddProfile(new BoardProfile());
                cfg.AddProfile(new ColumnProfile());
                cfg.AddProfile(new ColumnEdgeProfile());
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
        public async Task CreateAsync_ValidEdge_Persists_AndReturnedByGet()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;

            var boardId1 = await _boardService.CreateAsync("Testboard 1");
            var boardId2 = await _boardService.CreateAsync("Testboard 2");
            var columnId1 = new ColumnId(Guid.Parse("00000000-0000-0000-0000-000000000001"));
            var columnId2 = new ColumnId(Guid.Parse("00000000-0000-0000-0000-000000000002"));

            var actor = new Actor("-1", "Actor");

            var columnDto1 = new CreateColumnDto(columnId1, "Column 1", 0, boardId1, true);
            var columnDto2 = new CreateColumnDto(columnId2, "Column 2", 0, boardId2, true);

            await _columnService.CreateAsync(columnDto1, Guid.NewGuid(), actor, cancellationToken);
            await _columnService.CreateAsync(columnDto2, Guid.NewGuid(), actor, cancellationToken);

            // Act
            var columnEdgeDto = new CreateColumnEdgeDto(columnId2, columnId1);

            await _columnEdgeService.CreateAsync(columnEdgeDto);

            // Assert
            var edge = Assert.Single(await _columnEdgeService.GetByBoardIdAsync(boardId1));
            Assert.Equal(columnId1, edge.FromColumnId);
            Assert.Equal(columnId2, edge.ToColumnId);

            var edgeFromOtherBoard = Assert.Single(await _columnEdgeService.GetByBoardIdAsync(boardId2));
            Assert.Equal(columnId1, edgeFromOtherBoard.FromColumnId);
            Assert.Equal(columnId2, edgeFromOtherBoard.ToColumnId);
        }

        [Fact]
        public async Task CreateAsync_SelfEdge_Throws_AndNothingPersisted()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;

            var boardId1 = await _boardService.CreateAsync("Testboard 1");
            var columnId1 = new ColumnId(Guid.Parse("00000000-0000-0000-0000-000000000001"));

            var actor = new Actor("-1", "Actor");

            var columnDto1 = new CreateColumnDto(columnId1, "Column 1", 0, boardId1, true);

            await _columnService.CreateAsync(columnDto1, Guid.NewGuid(), actor, cancellationToken);

            // Act & Assert
            var columnEdgeDto = new CreateColumnEdgeDto(columnId1, columnId1);

            await Assert.ThrowsAsync<InvalidDomainException>(() => _columnEdgeService.CreateAsync(columnEdgeDto));

            Assert.Empty(await _columnEdgeService.GetByBoardIdAsync(boardId1));
        }

        [Fact]
        public async Task CreateAsync_DuplicateOrReverseEdge_ThrowsColumnEdgeAlreadyExists()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;

            var boardId1 = await _boardService.CreateAsync("Testboard 1");
            var boardId2 = await _boardService.CreateAsync("Testboard 2");
            var columnId1 = new ColumnId(Guid.Parse("00000000-0000-0000-0000-000000000001"));
            var columnId2 = new ColumnId(Guid.Parse("00000000-0000-0000-0000-000000000002"));

            var actor = new Actor("-1", "Actor");

            var columnDto1 = new CreateColumnDto(columnId1, "Column 1", 0, boardId1, true);
            var columnDto2 = new CreateColumnDto(columnId2, "Column 2", 0, boardId2, true);

            await _columnService.CreateAsync(columnDto1, Guid.NewGuid(), actor, cancellationToken);
            await _columnService.CreateAsync(columnDto2, Guid.NewGuid(), actor, cancellationToken);

            // Act & Assert
            var columnEdgeDto1 = new CreateColumnEdgeDto(columnId2, columnId1);
            var columnEdgeDto2 = new CreateColumnEdgeDto(columnId1, columnId2);

            await _columnEdgeService.CreateAsync(columnEdgeDto1);

            await Assert.ThrowsAsync<ColumnEdgeAlreadyExistsException>(() => _columnEdgeService.CreateAsync(columnEdgeDto1));
            await Assert.ThrowsAsync<ColumnEdgeAlreadyExistsException>(() => _columnEdgeService.CreateAsync(columnEdgeDto2));

            var edge = Assert.Single(await _columnEdgeService.GetByBoardIdAsync(boardId1));
            Assert.Equal(columnId1, edge.FromColumnId);
            Assert.Equal(columnId2, edge.ToColumnId);
        }

        [Fact]
        public async Task CreateAsync_UnknownFromColumn_ThrowsFK547()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;

            var boardId1 = await _boardService.CreateAsync("Testboard 1");
            var columnId1 = new ColumnId(Guid.Parse("00000000-0000-0000-0000-000000000001"));
            var columnId2 = new ColumnId(Guid.Parse("00000000-0000-0000-0000-000000000002"));

            var actor = new Actor("-1", "Actor");

            var columnDto1 = new CreateColumnDto(columnId2, "Column 1", 0, boardId1, true);

            await _columnService.CreateAsync(columnDto1, Guid.NewGuid(), actor, cancellationToken);

            // Act & Assert
            var columnEdgeDto = new CreateColumnEdgeDto(columnId1, columnId2);

            var ex = await Assert.ThrowsAsync<SqlException>(() => _columnEdgeService.CreateAsync(columnEdgeDto));

            Assert.Equal(547, ex.Number);

            Assert.Empty(await _columnEdgeService.GetByBoardIdAsync(boardId1));
        }

        [Fact]
        public async Task GetByBoardIdAsync_IncludesCrossBoardEdges()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;

            var boardId1 = await _boardService.CreateAsync("Testboard 1");
            var boardId2 = await _boardService.CreateAsync("Testboard 2");
            var boardId3 = await _boardService.CreateAsync("Testboard 3");
            var columnId1 = new ColumnId(Guid.Parse("00000000-0000-0000-0000-000000000001"));
            var columnId2 = new ColumnId(Guid.Parse("00000000-0000-0000-0000-000000000002"));

            var actor = new Actor("-1", "Actor");

            await _columnService.CreateAsync(
                new CreateColumnDto(columnId1, "Column 1", 0, boardId1, true),
                Guid.NewGuid(), actor, cancellationToken);

            await _columnService.CreateAsync(
                new CreateColumnDto(columnId2, "Column 2", 0, boardId2, true),
                Guid.NewGuid(), actor, cancellationToken);

            await _columnEdgeService.CreateAsync(new CreateColumnEdgeDto(columnId1, columnId2));

            // Act & Assert
            var fromBoard1 = Assert.Single(await _columnEdgeService.GetByBoardIdAsync(boardId1));
            var fromBoard2 = Assert.Single(await _columnEdgeService.GetByBoardIdAsync(boardId2));

            Assert.Equal(columnId1, fromBoard1.FromColumnId);
            Assert.Equal(columnId2, fromBoard1.ToColumnId);
            Assert.Equal(fromBoard1.FromColumnId, fromBoard2.FromColumnId);
            Assert.Equal(fromBoard1.ToColumnId, fromBoard2.ToColumnId);

            Assert.Empty(await _columnEdgeService.GetByBoardIdAsync(boardId3));
        }

        [Fact]
        public async Task DeleteAsync_ExistingEdge_ReturnsTrue_AndRemovesRow()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;

            var boardId1 = await _boardService.CreateAsync("Testboard 1");
            var boardId2 = await _boardService.CreateAsync("Testboard 2");
            var columnId1 = new ColumnId(Guid.Parse("00000000-0000-0000-0000-000000000001"));
            var columnId2 = new ColumnId(Guid.Parse("00000000-0000-0000-0000-000000000002"));

            var actor = new Actor("-1", "Actor");

            var columnDto1 = new CreateColumnDto(columnId1, "Column 1", 0, boardId1, true);
            var columnDto2 = new CreateColumnDto(columnId2, "Column 2", 0, boardId2, true);

            await _columnService.CreateAsync(columnDto1, Guid.NewGuid(), actor, cancellationToken);
            await _columnService.CreateAsync(columnDto2, Guid.NewGuid(), actor, cancellationToken);

            // Act
            var columnEdgeDto = new CreateColumnEdgeDto(columnId2, columnId1);

            await _columnEdgeService.CreateAsync(columnEdgeDto);

            // Assert
            var columnEdgeToDelete = ColumnEdge.Create(columnId2, columnId1);

            var deletedEdge = await _columnEdgeService.DeleteAsync(columnEdgeToDelete);

            Assert.False(await _columnEdgeService.DeleteAsync(ColumnEdge.Create(columnId1, columnId2)));

            Assert.True(deletedEdge);

            Assert.Empty(await _columnEdgeService.GetByBoardIdAsync(boardId1));
            Assert.Empty(await _columnEdgeService.GetByBoardIdAsync(boardId2));
        }
    }
}
