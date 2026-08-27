using kanban_lia.Infrastructure.Repositories.Boards;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Placements;
using kanban_lia.Services.Boards.DTOs;
using kanban_lia.Services.Boards.Exceptions;

namespace kanban_lia.Services.Boards
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
            var board = await _repository.GetByIdAsync(id);

            if (board is null)
            {
                throw new BoardNotFoundException(id);
            }

            return board;
        }

        public async Task<bool> RenameAsync(RenameBoardDto dto)
        {
            var boardId = new BoardId(dto.Id);

            var board = await _repository.GetByIdAsync(boardId);

            if (board is null)
            {
                throw new BoardNotFoundException(boardId);
            }

            board.Rename(dto.NewTitle);

            var renamed = await _repository.RenameAsync(
                boardId,
                board.Title);

            if (!renamed)
            {
                throw new BoardNotFoundException(boardId);
            }

            return renamed;
        }

        public async Task<bool> AddRootAsync(AddRootDto dto)
        {
            var boardId = new BoardId(dto.BoardId);
            var entityId = new EntityId(dto.EntityId);

            var boardExists = await _repository.BoardExistsAsync(boardId);
            var rootExists = await _repository.RootExistsAsync(boardId, entityId);

            var added = await _repository.AddRootAsync(boardId, entityId);

            if (!boardExists)
            {
                throw new BoardNotFoundException(boardId);
            }

            if (!rootExists)
            {
                throw new RootNotFoundException(entityId);
            }

            return added;
        }

        public async Task<bool> RemoveRootAsync(RemoveRootDto dto)
        {
            var boardId = new BoardId(dto.BoardId);
            var entityId = new EntityId(dto.EntityId);

            var boardExists = await _repository.BoardExistsAsync(boardId);
            var rootExists = await _repository.RootExistsAsync(boardId, entityId);

            var removed = await _repository.RemoveRootAsync(boardId, entityId);

            if (!boardExists)
            {
                throw new BoardNotFoundException(boardId);
            }

            if (!rootExists)
            {
                throw new RootNotFoundException(entityId);
            }

            return removed;
        }

        public async Task<bool> DeleteAsync(BoardId id)
        {
            var deleted = await _repository.DeleteAsync(id);

            if (!deleted)
            {
                throw new BoardNotFoundException(id);
            }

            return deleted;
        }
    }
}
