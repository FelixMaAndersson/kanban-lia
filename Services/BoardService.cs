using kanban_lia.Domain;
using kanban_lia.Infrastructure.Repositories;

namespace kanban_lia.Services
{
    public class BoardService : IBoardService
    {
        private readonly BoardRepository _repository;

        public BoardService(BoardRepository repository)
        {
            _repository = repository;
        }

        public async Task CreateAsync(Board board)
        {
            await _repository.CreateBoardAsync(board);
        }

        public async Task<IEnumerable<Board>> GetAllBoardsAsync()
        {
            return await _repository.GetAllBoardsAsync();
        }

        public async Task<Board?> GetBoardByIdAsync(Guid id)
        {
            return await _repository.GetBoardByIdAsync(id);
        }

        public async Task UpdateAsync(Board board)
        {
            await _repository.UpdateBoardAsync(board);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _repository.DeleteBoardAsync(id);
        }
    }
}
