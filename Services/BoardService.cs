using kanban_lia.Domain;
using kanban_lia.Infrastructure.Repositories;

namespace kanban_lia.Services
{
    public class BoardService(BoardRepository repository) : IBoardService
    {
        private readonly BoardRepository _repository = repository;

        public async Task CreateAsync(Board board)
        {
            await _repository.CreateAsync(board);
        }

        public async Task<IEnumerable<Board>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Board?> GetByIdAsync(BoardId id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task UpdateAsync(Board board)
        {
            await _repository.UpdateAsync(board);
        }

        public async Task DeleteAsync(BoardId id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}
