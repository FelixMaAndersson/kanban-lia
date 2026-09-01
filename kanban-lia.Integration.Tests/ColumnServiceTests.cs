using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;

using kanban_lia.Infrastructure.Repositories.Boards;
using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Mappings;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Services.Boards;
using kanban_lia.Services.Columns;
using kanban_lia.Services.Columns.DTOs;
using kanban_lia.Services.Columns.Exceptions;

namespace kanban_lia.Integration.Tests
{
    public class ColumnServiceTests(DatabaseFixture db) : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _db = db;
        private static readonly IMapper _mapper = new MapperConfiguration(
            cfg =>
            {
                cfg.AddProfile(new ColumnProfile());
            },
            NullLoggerFactory.Instance
        ).CreateMapper();

        private ColumnService CreateColumnService()
        {
            return new ColumnService(new ColumnRepository(_db.DbFactory), _mapper);
        }

        private BoardService CreateBoardService()
        {
            return new BoardService(new BoardRepository(_db.DbFactory), _mapper);
        }

        [Fact]
        public async Task CreateAsync_WithValidId_PersistsColumnThatCanBeRetrieved()
        {
            var columnService = CreateColumnService();
            var boardService = CreateBoardService();

            var boardId = await boardService.CreateAsync("Test Board");
            var columnId = new ColumnId(Guid.NewGuid());

            await columnService.CreateAsync(new CreateColumnDto(columnId, "Test Column", 1, boardId));

            var retrievedColumn = await columnService.GetByIdAsync(columnId);

            Assert.Equal("Test Column", retrievedColumn!.Title);
            Assert.Equal(columnId.Id, retrievedColumn.Id);
        }

        [Fact]
        public async Task GetByBoardIdAsync_ReturnsAllColumnsForBoard()
        {
            var columnService = CreateColumnService();
            var boardService = CreateBoardService();

            var boardId = await boardService.CreateAsync("Test Board");

            await columnService.CreateAsync(new CreateColumnDto(new ColumnId(Guid.NewGuid()), "Column 1", 1, boardId));
            await columnService.CreateAsync(new CreateColumnDto(new ColumnId(Guid.NewGuid()), "Column 2", 2, boardId));

            var columns = await columnService.GetByBoardIdAsync(boardId);

            Assert.Equal(2, columns.Count());
            Assert.Contains(columns, c => c.Title == "Column 1");
            Assert.Contains(columns, c => c.Title == "Column 2");
        }

        [Fact]
        public async Task RenameAsync_WithValidId_UpdatesColumnTitle()
        {
            var columnService = CreateColumnService();
            var boardService = CreateBoardService();

            var boardId = await boardService.CreateAsync("Test Board");
            var columnId = new ColumnId(Guid.NewGuid());

            await columnService.CreateAsync(new CreateColumnDto(columnId, "Old Title", 1, boardId));
            var renamed = await columnService.RenameAsync(new RenameColumnDto(columnId.Id, "New Title"));

            var updatedColumn = await columnService.GetByIdAsync(columnId);

            Assert.True(renamed);
            Assert.Equal("New Title", updatedColumn!.Title);
        }

        [Fact]
        public async Task DeleteAsync_DeletesColumn()
        {
            var columnService = CreateColumnService();
            var boardService = CreateBoardService();

            var boardId = await boardService.CreateAsync("Test Board");
            var columnId = new ColumnId(Guid.NewGuid());

            await columnService.CreateAsync(new CreateColumnDto(columnId, "Old Title", 1, boardId));
            var deleted = await columnService.DeleteAsync(columnId);

            Assert.True(deleted);
            await Assert.ThrowsAsync<ColumnNotFoundException>(() => columnService.GetByIdAsync(columnId));
        }
    }
}
