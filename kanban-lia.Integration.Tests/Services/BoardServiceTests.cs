using AutoMapper;
using kanban_lia.Infrastructure.Repositories.Boards;
using kanban_lia.Integration.Tests.Fixture;
using kanban_lia.Mappings;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Placements;
using kanban_lia.Services.Boards;
using kanban_lia.Services.Boards.DTOs;
using kanban_lia.Services.Boards.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;

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
            },
            NullLoggerFactory.Instance).CreateMapper();

        private readonly BoardRepository _repository = new(database.DbFactory);
        private readonly BoardService _service = new(
            new BoardRepository(database.DbFactory),
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
            var board = await _repository.GetByIdAsync(id);

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
            var board = await _repository.GetByIdAsync(id);

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
    }
}
