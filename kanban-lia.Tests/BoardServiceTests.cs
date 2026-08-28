using Moq;

using kanban_lia.Infrastructure.Repositories.Boards;
using kanban_lia.Services.Boards;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Services.Boards.Exceptions;
using kanban_lia.Services.Boards.DTOs;
using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Tests
{
    [TestClass]
    public class BoardServiceTests
    {
        private Mock<IBoardRepository> _mockRepository = null!;
        private BoardService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockRepository = new Mock<IBoardRepository>();
            _service = new BoardService(_mockRepository.Object);
        }

        [TestMethod]
        public async Task CreateAsync_WithValidId_CreatesBoard()
        {
            var id = new BoardId(Guid.NewGuid());

            _mockRepository.Setup(r => r.CreateAsync(It.IsAny<Board>())).ReturnsAsync(id);

            await _service.CreateAsync("Testboard");

            _mockRepository.Verify(r => r.CreateAsync(It.Is<Board>(b => b.Title == "Testboard")), Times.Once);
        }

        [TestMethod]
        public async Task GetById_WithExistingBoard_ReturnsBoard()
        {
            var id = new BoardId(Guid.NewGuid());
            var board = Board.Create("Testboard");

            _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(board);

            Assert.AreEqual(board, await _service.GetByIdAsync(id));
        }

        [TestMethod]
        public async Task GetById_WithNonExistingBoard_ThrowsException()
        {
            var id = new BoardId(Guid.NewGuid());

            _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Board?)null);

            await Assert.ThrowsExactlyAsync<BoardNotFoundException>(() => _service.GetByIdAsync(id));
        }

        [TestMethod]
        public async Task RenameAsync_WithExistingBoard_RenamesBoard()
        {
            var id = new BoardId(Guid.NewGuid());
            var board = Board.Create("Testboard");

            _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(board);
            _mockRepository.Setup(r => r.RenameAsync(id, "Renamedboard")).ReturnsAsync(true);

            await _service.RenameAsync(new RenameBoardDto(id.Value, "Renamedboard"));

            _mockRepository.Verify(r => r.RenameAsync(id, "Renamedboard"), Times.Once);
        }

        [TestMethod]
        public async Task RenameAsync_WithNonExistingBoard_ThrowsException()
        {
            var id = new BoardId(Guid.NewGuid());

            _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Board?)null);

            await Assert.ThrowsExactlyAsync<BoardNotFoundException>(() => _service.RenameAsync(new RenameBoardDto(id.Value, "Renamedboard")));
        }

        [TestMethod]
        public async Task RenameAsync_WhenRepoFails_ThrowsException()
        {
            var id = new BoardId(Guid.NewGuid());
            var board = Board.Create("Testboard");

            _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(board);
            _mockRepository.Setup(r => r.RenameAsync(id, "Renamedboard")).ReturnsAsync(false);

            await Assert.ThrowsExactlyAsync<BoardNotFoundException>(() => _service.RenameAsync(new RenameBoardDto(id.Value, "Renamedboard")));
        }

        [TestMethod]
        public async Task AddRootAsync_WithExistingBoard_AddsRoot()
        {
            var boardId = new BoardId(Guid.NewGuid());
            var entityId = new EntityId(Guid.NewGuid());

            _mockRepository.Setup(r => r.BoardExistsAsync(boardId)).ReturnsAsync(true);
            _mockRepository.Setup(r => r.RootExistsAsync(boardId, entityId)).ReturnsAsync(false);

            await _service.AddRootAsync(new AddRootDto(boardId.Value, entityId.Value));

            _mockRepository.Verify(r => r.AddRootAsync(boardId, entityId), Times.Once);
        }

        [TestMethod]
        public async Task AddRootAsync_WithNonExistingBoard_ThrowsException()
        {
            var boardId = new BoardId(Guid.NewGuid());
            var entityId = new EntityId(Guid.NewGuid());

            _mockRepository.Setup(r => r.BoardExistsAsync(boardId)).ReturnsAsync(false);

            await Assert.ThrowsExactlyAsync<BoardNotFoundException>(() => _service.AddRootAsync(new AddRootDto(boardId.Value, entityId.Value)));
        }

        [TestMethod]
        public async Task AddRootAsync_WithDuplicateRoot_ThrowsException()
        {
            var boardId = new BoardId(Guid.NewGuid());
            var entityId = new EntityId(Guid.NewGuid());

            _mockRepository.Setup(r => r.BoardExistsAsync(boardId)).ReturnsAsync(true);
            _mockRepository.Setup(r => r.RootExistsAsync(boardId, entityId)).ReturnsAsync(true);

            await Assert.ThrowsExactlyAsync<RootAlreadyExistsException>(() => _service.AddRootAsync(new AddRootDto(boardId.Value, entityId.Value)));
        }

        [TestMethod]
        public async Task RemoveRootAsync_WithExistingBoard_RemovesRoot()
        {
            var boardId = new BoardId(Guid.NewGuid());
            var entityId = new EntityId(Guid.NewGuid());

            _mockRepository.Setup(r => r.BoardExistsAsync(boardId)).ReturnsAsync(true);
            _mockRepository.Setup(r => r.RootExistsAsync(boardId, entityId)).ReturnsAsync(true);

            await _service.RemoveRootAsync(new RemoveRootDto(boardId.Value, entityId.Value));

            _mockRepository.Verify(r => r.RemoveRootAsync(boardId, entityId), Times.Once);
        }

        [TestMethod]
        public async Task RemoveRootAsync_WithNonExistingBoard_ThrowsException()
        {
            var boardId = new BoardId(Guid.NewGuid());
            var entityId = new EntityId(Guid.NewGuid());

            _mockRepository.Setup(r => r.BoardExistsAsync(boardId)).ReturnsAsync(false);

            await Assert.ThrowsExactlyAsync<BoardNotFoundException>(() => _service.RemoveRootAsync(new RemoveRootDto(boardId.Value, entityId.Value)));
        }

        [TestMethod]
        public async Task RemoveRootAsync_WithNoRoot_ThrowsException()
        {
            var boardId = new BoardId(Guid.NewGuid());
            var entityId = new EntityId(Guid.NewGuid());

            _mockRepository.Setup(r => r.BoardExistsAsync(boardId)).ReturnsAsync(true);
            _mockRepository.Setup(r => r.RootExistsAsync(boardId, entityId)).ReturnsAsync(false);

            await Assert.ThrowsExactlyAsync<RootNotFoundException>(() => _service.RemoveRootAsync(new RemoveRootDto(boardId.Value, entityId.Value)));
        }

        [TestMethod]
        public async Task DeleteAsync_WithValidId_DeletesBoard()
        {
            var id = new BoardId(Guid.NewGuid());

            _mockRepository.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

            await _service.DeleteAsync(id);

            _mockRepository.Verify(r => r.DeleteAsync(id), Times.Once);
        }

        [TestMethod]
        public async Task DeleteAsync_WhenRepoFails_ThrowsException()
        {
            var id = new BoardId(Guid.NewGuid());

            _mockRepository.Setup(r => r.DeleteAsync(id)).ReturnsAsync(false);

            await Assert.ThrowsExactlyAsync<BoardNotFoundException>(() => _service.DeleteAsync(id));
        }
    }
}
