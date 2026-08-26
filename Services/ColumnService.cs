using kanban_lia.Infrastructure.Repositories;
using kanban_lia.Models.Domain;
using kanban_lia.Services.DTOs;

namespace kanban_lia.Services
{
    public class ColumnService(ColumnRepository repository) : IColumnService
    {
        private readonly ColumnRepository _repository = repository;

        public async Task CreateAsync(CreateColumnDto dto)
        {
            var boardId = new BoardId(dto.BoardId);

            var newColumn = Column.Create(dto.Title, dto.Position, boardId);

            await _repository.CreateAsync(newColumn);
        }

        public async Task DeleteAsync(ColumnId id)
        {
            await _repository.DeleteAsync(id);
        }

        public async Task<IEnumerable<Column>> GetByBoardIdAsync(BoardId boardId)
        {
            return await _repository.GetByBoardIdAsync(boardId);
        }

        public async Task<Column?> GetByIdAsync(ColumnId id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task RenameAsync(RenameColumnDto dto)
        {
            var columnId = new ColumnId(dto.Id);

            await _repository.RenameAsync(columnId, dto.NewTitle);
        }
    }
}

