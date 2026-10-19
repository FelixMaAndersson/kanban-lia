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
using kanban_lia.Services.Boards.DTOs;
using kanban_lia.Services.Boards.Exceptions;
using kanban_lia.Services.Columns;
using kanban_lia.Services.Columns.DTOs;
using kanban_lia.Services.Placements;
using Actor = kanban_lia.Services.IntegrationEvents.Models.Actor;
using Microsoft.Extensions.Logging.Abstractions;
using kanban_lia.Services.Placements.DTOs;


namespace kanban_lia.Integration.Tests.Services
{
    /// <summary>
    /// Class <c>BoardServiceTests</c> is the test class for <c>BoardService</c>
    /// </summary>
    /// <param name="database">Primary constructor for <c>DatabaseFixture</c></param>
    [Collection(DatabaseCollection.Name)]
    public sealed class BoardServiceTests(DatabaseFixture database) : IAsyncLifetime
    {
        private readonly DatabaseFixture _database = database;

        private static readonly IMapper _mapper = new MapperConfiguration(
            cfg =>
            {
                cfg.AddProfile(new BoardProfile());
                cfg.AddProfile(new ColumnProfile());
                cfg.AddProfile(new ColumnEdgeProfile());
                cfg.AddProfile(new PlacementProfile());
            },
            NullLoggerFactory.Instance).CreateMapper();

        private readonly BoardRepository _repository = new(database.DbFactory);
        private readonly ColumnRepository _columnRepository = new(database.DbFactory);
        private readonly BoardService _service = new(
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
        public async Task CreateAsync_WithValidProperties_CreatesBoard()
        {
            // Act
            var id = await _service.CreateAsync("Testboard");

            // Assert
            var board = await _service.GetByIdAsync(id);

            Assert.NotNull(board);
            Assert.Equal("Testboard", board.Title);
        }

        [Fact]
        public async Task GetById_WithExistingBoard_ReturnsBoard()
        {
            // Arrange
            var id = await _service.CreateAsync("Testboard");

            // Act
            var board = await _service.GetByIdAsync(id);

            // Assert
            Assert.NotNull(board);
            Assert.Equal(id, board.Id);
        }

        [Fact]
        public async Task GetById_WithNonExistingBoard_ThrowsException()
        {
            // Arrange
            var id = new BoardId(Guid.NewGuid());

            // Act and Assert
            await Assert.ThrowsAsync<BoardNotFoundException>(() => _service.GetByIdAsync(id));
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsBoardWithRootsLoaded()
        {
            // Arrange
            var boardId = await _service.CreateAsync("Testboard");
            var entityId = new EntityId(Guid.NewGuid());

            await _service.AddRootAsync(new AddRootDto(boardId, entityId));

            // Act
            var board = await _service.GetByIdAsync(boardId);

            // Assert
            Assert.NotNull(board);
            Assert.NotEmpty(board.Roots);
            Assert.Contains(entityId, board.Roots);
        }

        [Fact]
        public async Task RenameAsync_WithExistingBoard_RenamesBoard()
        {
            // Arrange
            var id = await _service.CreateAsync("Testboard");

            // Act
            await _service.RenameAsync(new RenameBoardDto(id, "Renamedboard"));

            // Assert
            var board = await _service.GetByIdAsync(id);

            Assert.NotNull(board);
            Assert.Equal("Renamedboard", board.Title);
        }

        [Fact]
        public async Task RenameAsync_WithNonExistingBoard_ThrowsException()
        {
            // Arrange
            var id = new BoardId(Guid.NewGuid());

            // Act and Assert
            await Assert.ThrowsAsync<BoardNotFoundException>(() => _service.RenameAsync(new RenameBoardDto(id, "Renamedboard")));
        }

        [Fact]
        public async Task AddRootAsync_WithExistingBoard_AddsRoot()
        {
            // Arrange
            var boardId = await _service.CreateAsync("Testboard");
            var entityId = new EntityId(Guid.NewGuid());

            // Act
            await _service.AddRootAsync(new AddRootDto(boardId, entityId));

            // Assert
            var rootExists = await _repository.RootExistsAsync(
                boardId,
                entityId);

            Assert.True(rootExists);
        }

        [Fact]
        public async Task AddRootAsync_WithNonExistingBoard_ThrowsException()
        {
            // Arrange
            var boardId = new BoardId(Guid.NewGuid());
            var entityId = new EntityId(Guid.NewGuid());

            // Act and Assert
            await Assert.ThrowsAsync<BoardNotFoundException>(() => _service.AddRootAsync(new AddRootDto(boardId, entityId)));
        }

        [Fact]
        public async Task AddRootAsync_WithDuplicateRoot_ThrowsException()
        {
            // Arrange
            var boardId = await _service.CreateAsync("Testboard");
            var entityId = new EntityId(Guid.NewGuid());

            await _service.AddRootAsync(new AddRootDto(boardId, entityId));

            // Act and Assert
            await Assert.ThrowsAsync<RootAlreadyExistsException>(() => _service.AddRootAsync(new AddRootDto(boardId, entityId)));
        }

        [Fact]
        public async Task RemoveRootAsync_WithExistingBoard_RemovesRoot()
        {
            // Arrange
            var boardId = await _service.CreateAsync("Testboard");
            var entityId = new EntityId(Guid.NewGuid());

            await _service.AddRootAsync(new AddRootDto(boardId, entityId));

            // Act
            await _service.RemoveRootAsync(new RemoveRootDto(boardId, entityId));

            // Assert
            var rootExists = await _repository.RootExistsAsync(
                boardId,
                entityId);

            Assert.False(rootExists);
        }

        [Fact]
        public async Task RemoveRootAsync_WithNonExistingBoard_ThrowsException()
        {
            // Arrange
            var boardId = new BoardId(Guid.NewGuid());
            var entityId = new EntityId(Guid.NewGuid());

            // Act and Assert
            await Assert.ThrowsAsync<BoardNotFoundException>(() => _service.RemoveRootAsync(new RemoveRootDto(boardId, entityId)));
        }

        [Fact]
        public async Task RemoveRootAsync_WithNoRoot_ThrowsException()
        {
            // Arrange
            var boardId = await _service.CreateAsync("Testboard");
            var entityId = new EntityId(Guid.NewGuid());

            // Act and Assert
            await Assert.ThrowsAsync<RootNotFoundException>(() => _service.RemoveRootAsync(new RemoveRootDto(boardId, entityId)));
        }

        [Fact]
        public async Task DeleteAsync_WithExistingBoard_DeletesBoard()
        {
            // Arrange
            var id = await _service.CreateAsync("Testboard");

            // Act
            await _service.DeleteAsync(id);

            // Assert
            var board = await _repository.GetByIdAsync(id);

            Assert.Null(board);
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistingBoard_ThrowsException()
        {
            // Arrange
            var id = new BoardId(Guid.NewGuid());

            // Act and Assert
            await Assert.ThrowsAsync<BoardNotFoundException>(() => _service.DeleteAsync(id));
        }

        [Fact]
        public async Task DeleteAsync_RemovesColumnsEdgesAndPlacements()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;

            var boardId1 = await _service.CreateAsync("Board1");
            var boardId2 = await _service.CreateAsync("Board2");

            var columnId1 = new ColumnId(Guid.NewGuid());
            var columnId2 = new ColumnId(Guid.NewGuid());

            var columnDto1 = new CreateColumnDto(columnId1, "TestCol", 0, boardId1, true);
            var columnDto2 = new CreateColumnDto(columnId2, "TestCol", 0, boardId2, true);

            var actor = new Actor("-1", "Actor");

            await _columnService.CreateAsync(columnDto1, Guid.NewGuid(), actor, cancellationToken);

            await _columnService.CreateAsync(columnDto2, Guid.NewGuid(), actor, cancellationToken);

            var columnEdgeDto1 = new CreateColumnEdgeDto(columnId1, columnId2);
            var columnEdgeDto2 = new CreateColumnEdgeDto(columnId2, columnId1);

            await _columnEdgeService.CreateAsync(columnEdgeDto1);
            await _columnEdgeService.CreateAsync(columnEdgeDto2);

            var entityId1 = new EntityId(Guid.NewGuid());
            var entityId2 = new EntityId(Guid.NewGuid());

            var placementOp1 = new PlacementOperationDto(
                new CreatePlacementDto([entityId1], boardId1, columnId1, [], []),
                []);
            var placementOp2 = new PlacementOperationDto(
                new CreatePlacementDto([entityId2], boardId2, columnId2, [], []),
                []);

            await _placementService.CreateAsync(
                [placementOp1, placementOp2],
                Guid.NewGuid(),
                null,
                actor,
                cancellationToken);

            // Act
            await _service.DeleteAsync(boardId1);

            // Assert
            Assert.False(await _repository.BoardExistsAsync(boardId1));
            Assert.Null(await _columnRepository.GetByIdAsync(columnId1));
            Assert.Empty(await _columnEdgeService.GetByBoardIdAsync(boardId1));
            Assert.Empty(await _placementService.GetCurrentByBoardAsync(boardId1));

            Assert.True(await _repository.BoardExistsAsync(boardId2));
            Assert.NotNull(await _columnRepository.GetByIdAsync(columnId2));
            Assert.Empty(await _columnEdgeService.GetByBoardIdAsync(boardId2));
            Assert.Single(await _placementService.GetCurrentByBoardAsync(boardId2));
        }
    }
}
