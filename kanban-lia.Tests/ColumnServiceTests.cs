using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Services.Columns;
using kanban_lia.Services.Columns.DTOs;
using kanban_lia.Services.Columns.Exceptions;
using Moq;

namespace kanban_lia.Tests
{
    [TestClass]
    public class ColumnServiceTests
    {
        private Mock<IColumnRepository> _mockRepository = null!;
        private ColumnService _columnService = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockRepository = new Mock<IColumnRepository>();
            _columnService = new ColumnService(_mockRepository.Object);
        }

        [TestMethod]
        public async Task CreateAsync_WithValidDto_CallsRepositoryOnce()
        {
            var dto = new CreateColumnDto("New Column", 1, Guid.NewGuid());
            
            await _columnService.CreateAsync(dto);

            _mockRepository.Verify(r => r.CreateAsync(It.Is<Column>(c =>
                c.Title == dto.Title &&
                c.Position == dto.Position &&
                c.BoardId.Value == dto.BoardId)), Times.Once);
        }

        [TestMethod]
        public async Task GetByBoardIdAsync_WithValidBoardId_CallsRepositoryOnce()
        {
            var boardId = new BoardId(Guid.NewGuid());

            await _columnService.GetByBoardIdAsync(boardId);

            _mockRepository.Verify(r => r.GetByBoardIdAsync(boardId), Times.Once);
        }

        [TestMethod]
        public async Task GetByIdAsync_WithValidId_CallsRepositoryOnce()
        {
            var columnId = new ColumnId(Guid.NewGuid());
            var column = Column.Create("Some Column", 0, new BoardId(Guid.NewGuid()));

            _mockRepository.Setup(r => r.GetByIdAsync(columnId))
                .ReturnsAsync(column);

            var result = await _columnService.GetByIdAsync(columnId);

            Assert.AreSame(column, result);
            _mockRepository.Verify(r => r.GetByIdAsync(columnId), Times.Once);
        }

        [TestMethod]
        public async Task GetByIdAsync_WithInvalidId_ThrowsColumnNotFoundException()
        {
            var columnId = new ColumnId(Guid.NewGuid());
            _mockRepository.Setup(r => r.GetByIdAsync(columnId))
                .ReturnsAsync((Column?)null);
            await Assert.ThrowsExactlyAsync<ColumnNotFoundException>(() => _columnService.GetByIdAsync(columnId));
        }

        [TestMethod]
        public async Task RenameAsync_WithValidDto_CallsRepositoryOnce()
        {
            _mockRepository.Setup(r => r.RenameAsync(It.IsAny<ColumnId>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            var dto = new RenameColumnDto(Guid.NewGuid(), "Updated Column");

            var result = await _columnService.RenameAsync(dto);

            Assert.IsTrue(result);
            _mockRepository.Verify(r => r.RenameAsync(
                It.Is<ColumnId>(id => id.Value == dto.Id),
                dto.NewTitle), Times.Once);
        }

        [TestMethod]
        public async Task RenameAsync_WithInvalidDto_ThrowsColumnNotFoundException()
        {
            _mockRepository.Setup(r => r.RenameAsync(It.IsAny<ColumnId>(), It.IsAny<string>()))
                .ReturnsAsync(false);
            var dto = new RenameColumnDto(Guid.NewGuid(), "Updated Column");
            await Assert.ThrowsExactlyAsync<ColumnNotFoundException>(() => _columnService.RenameAsync(dto));
        }

        [TestMethod]
        public async Task DeleteAsync_WithValidId_CallsRepositoryOnce()
        {
            _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<ColumnId>()))
                .ReturnsAsync(true);
            var columnId = new ColumnId(Guid.NewGuid());
            var result = await _columnService.DeleteAsync(columnId);
            Assert.IsTrue(result);
            _mockRepository.Verify(r => r.DeleteAsync(
                It.Is<ColumnId>(id => id.Value == columnId.Value)), Times.Once);
        }

        [TestMethod]
        public async Task DeleteAsync_WithInvalidId_ThrowsColumnNotFoundException()
        {
            var columnId = new ColumnId(Guid.NewGuid());
            _mockRepository.Setup(r => r.DeleteAsync(columnId))
                .ReturnsAsync(false);
            await Assert.ThrowsExactlyAsync<ColumnNotFoundException>(() => _columnService.DeleteAsync(columnId));
        }
    }
}
