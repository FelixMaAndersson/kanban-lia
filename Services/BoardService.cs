using kanban_lia.Domain;
using kanban_lia.Infrastructure.Repositories;

namespace kanban_lia.Services
{
    public class BoardService(BoardRepository repository) : IBoardService
    {
        private readonly IBoardRepository _repository = repository;

        public Task CreateAsync(Board board)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(BoardId id)
        {
            throw new NotImplementedException();
        }

        public async Task<Board?> GetByIdAsync(BoardId id)
        {
            throw new NotImplementedException();
        }

        public async Task<Board> RenameAsync(BoardId id, string title)
        {
            var existingBoard = await _repository.GetByIdAsync(id);
            existingBoard.Rename(title);
            _repository.Update(existingBoard);
            return existingBoard;
        }
        public async Task<Board> AddRootAsync(BoardId id, Guid entityId)
        {
            var existingBoard = await _repository.GetByIdAsync(id);
            existingBoard.AddRoot(entityId);
            _repository.Update(existingBoard);
            return existingBoard;
        }

        public async Task DeleteAsync(BoardId id)
        {
            throw new NotImplementedException();
        }
    }
}
