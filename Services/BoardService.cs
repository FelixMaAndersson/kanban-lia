using kanban_lia.Infrastructure.Repositories;
using kanban_lia.Models.Domain;
using kanban_lia.Services.DTOs;

namespace kanban_lia.Services
{
    public class BoardService(IBoardRepository repository) : IBoardService
    {
        private readonly IBoardRepository _repository = repository;

        public async Task CreateAsync(string title)
        {
            var newBoard = Board.Create(title);

            await _repository.CreateAsync(newBoard);
        }

        public async Task DeleteAsync(BoardId id)
        {
            await _repository.DeleteAsync(id);
        }

        public async Task<Board?> GetByIdAsync(BoardId id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task RenameAsync(RenameBoardDto dto)
        {
            var boardId = new BoardId(dto.Id);

            await _repository.RenameAsync(boardId, dto.NewTitle);
        }
    }
}
