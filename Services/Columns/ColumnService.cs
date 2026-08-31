using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Services.Columns.DTOs;
using kanban_lia.Services.Columns.Exceptions;

namespace kanban_lia.Services.Columns
{
    public class ColumnService(IColumnRepository repository) : IColumnService
    {
        private readonly IColumnRepository _repository = repository;

        public async Task CreateAsync(CreateColumnDto dto)
        {
            var newColumn = Column.Create(dto.Id, dto.Title, dto.Position, dto.BoardId);

            await _repository.CreateAsync(newColumn);
        }

        public async Task<IEnumerable<Column>> GetByBoardIdAsync(BoardId boardId)
        {
            return await _repository.GetByBoardIdAsync(boardId);
        }

        public async Task<Column?> GetByIdAsync(ColumnId id)
        {
            var column = await _repository.GetByIdAsync(id);

            if (column is null)
            {
                throw new ColumnNotFoundException(id.Id);
            }

            return column;
        }

        public async Task<bool> RenameAsync(RenameColumnDto dto)
        {
            var columnId = new ColumnId(dto.Id);

            var renamed = await _repository.RenameAsync(columnId, dto.NewTitle);

            if (!renamed)
            {
                throw new ColumnNotFoundException(columnId.Id);
            }

            return renamed;
        }

        public async Task<bool> DeleteAsync(ColumnId id)
        {
            var deleted = await _repository.DeleteAsync(id);

            if (!deleted)
            {
                throw new ColumnNotFoundException(id.Id);
            }

            return deleted;
        }
    }
}