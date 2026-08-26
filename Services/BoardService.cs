using kanban_lia.Infrastructure.Repositories;
using kanban_lia.Models.Domain;
using kanban_lia.Services.DTOs;

namespace kanban_lia.Services
{
    public class BoardService(IBoardRepository repository) : IBoardService
    {
        private readonly IBoardRepository _repository = repository;

        public async Task<BoardId> CreateAsync(string title)
        {
            var newBoard = Board.Create(title);

            return await _repository.CreateAsync(newBoard);
        }

        public async Task<Board?> GetByIdAsync(BoardId id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<bool> RenameAsync(RenameBoardDto dto)
        {
            var boardId = new BoardId(dto.Id);

            return await _repository.RenameAsync(boardId, dto.NewTitle);
        }

        public async Task<bool> AddRootAsync(AddRootDto dto)
        {
            var boardId = new BoardId(dto.BoardId);
            var entityId = new EntityId(dto.EntityId);

            return await _repository.AddRootAsync(boardId, entityId);
        }

        public async Task<bool> RemoveRootAsync(RemoveRootDto dto)
        {
            var boardId = new BoardId(dto.BoardId);
            var entityId = new EntityId(dto.EntityId);

            return await _repository.RemoveRootAsync(boardId, entityId);
        }

        public async Task<bool> DeleteAsync(BoardId id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}
