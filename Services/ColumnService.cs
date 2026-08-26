using kanban_lia.Domain;
using kanban_lia.Infrastructure.Repositories;

namespace kanban_lia.Services
{
    public class ColumnService(ColumnRepository repository) : IColumnService
    {
        private readonly ColumnRepository _repository = repository;

        public async Task CreateAsync(Column column)
        {
            await _repository.CreateAsync(column);
        }

        public async Task<IEnumerable<Column>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<IEnumerable<Column>> GetByBoardIdAsync(BoardId boardId)
        {
            return await _repository.GetByBoardIdAsync(boardId);
        }

        public async Task<Column?> GetByIdAsync(ColumnId id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task UpdateAsync(Column column)
        {
            await _repository.UpdateAsync(column);
        }

        public async Task DeleteAsync(ColumnId id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}
