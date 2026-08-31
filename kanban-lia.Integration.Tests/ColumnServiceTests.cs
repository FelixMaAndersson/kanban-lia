using kanban_lia.Infrastructure.Repositories.Boards;
using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Services.Columns;
using kanban_lia.Services.Columns.DTOs;
using kanban_lia.Services.Columns.Exceptions;

namespace kanban_lia.Integration.Tests
{
    public class ColumnServiceTests(DatabaseFixture db) : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _db = db;

        private ColumnService CreateService()
        {
            return new ColumnService(new ColumnRepository(_db.DbFactory));
        }

        private async Task<BoardId> CreateBoardAsync()
        {
            var boardRepo = new BoardRepository(_db.DbFactory);
            var board = Board.Create("Test Board");
            await boardRepo.CreateAsync(board);
            return board.Id;
        }

        [Fact]
        public async Task CreateAsync_WithValidId_PersistsColumnThatCanBeRetrieved()
        {
            var service = CreateService();

            var boardId = await CreateBoardAsync();
            var columnId = new ColumnId(Guid.NewGuid());

            await service.CreateAsync(new CreateColumnDto(columnId, "Test Column", 1, boardId));

            var retrievedColumn = await service.GetByIdAsync(columnId);

            Assert.Equal("Test Column", retrievedColumn!.Title);
            Assert.Equal(columnId, retrievedColumn.Id);
        }

        [Fact]
        public async Task GetByBoardIdAsync_ReturnsAllColumnsForBoard()
        {
            var service = CreateService();

            var boardId = await CreateBoardAsync();

            await service.CreateAsync(new CreateColumnDto(new ColumnId(Guid.NewGuid()), "Column 1", 1, boardId));
            await service.CreateAsync(new CreateColumnDto(new ColumnId(Guid.NewGuid()), "Column 2", 2, boardId));

            var columns = await service.GetByBoardIdAsync(boardId);

            Assert.Equal(2, columns.Count());
            Assert.Contains(columns, c => c.Title == "Column 1");
            Assert.Contains(columns, c => c.Title == "Column 2");
        }

        [Fact]
        public async Task RenameAsync_WithValidId_UpdatesColumnTitle()
        {
            var service = CreateService();

            var boardId = await CreateBoardAsync();
            var columnId = new ColumnId(Guid.NewGuid());

            await service.CreateAsync(new CreateColumnDto(columnId, "Old Title", 1, boardId));
            var renamed = await service.RenameAsync(new RenameColumnDto(columnId.Id, "New Title"));

            var updatedColumn = await service.GetByIdAsync(columnId);

            Assert.True(renamed);
            Assert.Equal("New Title", updatedColumn!.Title);
        }

        [Fact]
        public async Task DeleteAsync_DeletesColumn()
        {
            var service = CreateService();

            var boardId = await CreateBoardAsync();
            var columnId = new ColumnId(Guid.NewGuid());

            await service.CreateAsync(new CreateColumnDto(columnId, "Old Title", 1, boardId));
            var deleted = await service.DeleteAsync(columnId);

            Assert.True(deleted);
            await Assert.ThrowsAsync<ColumnNotFoundException>(() => service.GetByIdAsync(columnId));
        }
    }
}
