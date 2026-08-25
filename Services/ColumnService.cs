using kanban_lia.Domain;
using kanban_lia.Infrastructure.Repositories;

namespace kanban_lia.Services
{
    public class ColumnService : IColumnService
    {
        private readonly ColumnRepository _repository;

        public ColumnService(ColumnRepository repository)
        {
            _repository = repository;
        }

        public async Task CreateAsync(Column column)
        {
            await _repository.CreateColumnAsync(column);
        }

        public async Task<IEnumerable<Column>> GetAllColumnsAsync()
        {
            return await _repository.GetAllColumnsAsync();
        }

        public async Task<IEnumerable<Column>> GetColumnsByBoardIdAsync(Guid boardId)
        {
            return await _repository.GetColumnsByBoardIdAsync(boardId);
        }

        public async Task<Column?> GetColumnByIdAsync(Guid id)
        {
            return await _repository.GetColumnByIdAsync(id);
        }

        public async Task UpdateAsync(Column column)
        {
            await _repository.UpdateColumnAsync(column);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _repository.DeleteColumnAsync(id);
        }
    }
}
