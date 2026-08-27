using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Services.Columns.DTOs;

namespace kanban_lia.Services.Columns
{
    public class ColumnService(IColumnRepository repository) : IColumnService
    {
        private readonly IColumnRepository _repository = repository;

        public async Task<Column> CreateAsync(CreateColumnDto dto)
        {
            var boardId = new BoardId(dto.BoardId);

            var newColumn = Column.Create(dto.Title, dto.Position, boardId);

            return await _repository.CreateAsync(newColumn);
        }

        public async Task<IEnumerable<Column>> GetByBoardIdAsync(BoardId boardId)
        {
            return await _repository.GetByBoardIdAsync(boardId);
        }

        public async Task<Column?> GetByIdAsync(ColumnId id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<bool> RenameAsync(RenameColumnDto dto)
        {
            var columnId = new ColumnId(dto.Id);

            return await _repository.RenameAsync(columnId, dto.NewTitle);
        }

        public async Task<bool> DeleteAsync(ColumnId id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}

