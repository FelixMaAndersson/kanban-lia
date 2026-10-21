using AutoMapper;

using kanban_lia.Infrastructure.Repositories.Boards;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Boards.DTOs;
using kanban_lia.Services.Boards.DTOs;
using kanban_lia.Services.Boards.Exceptions;

namespace kanban_lia.Services.Boards
{
    /// <summary>
    /// Application service that coordinates board creation, lookup, renaming, root management, and deletion.
    /// </summary>
    /// <remarks>Each method translates repository results into domain outcomes, throwing a specific exception
    /// when an entity is missing or a precondition fails so callers can convert the failure into an HTTP
    /// response. Boards are loaded, mutated through their aggregate root, and persisted via
    /// <see cref="IBoardRepository"/>.</remarks>
    /// <param name="repository">Repository used to load and persist board aggregates and roots.</param>
    /// <param name="mapper">Mapper used to convert the <see cref="Board"/> aggregate into a
    /// <see cref="BoardDto"/> for callers.</param>
    public class BoardService(
        IBoardRepository repository,
        IMapper mapper) : IBoardService
    {
        private readonly IMapper _mapper = mapper;
        private readonly IBoardRepository _repository = repository;

        /// <summary>
        /// Creates a new board with the given title.
        /// </summary>
        /// <param name="title">The title for the new board; validated by <see cref="Board.Create"/>.</param>
        /// <returns>A task whose result is the identifier assigned to the newly created board.</returns>
        public async Task<BoardId> CreateAsync(string title)
        {
            var newBoard = Board.Create(title);

            return await _repository.CreateAsync(newBoard);
        }

        /// <summary>
        /// Retrieves a board by id and maps it to its DTO.
        /// </summary>
        /// <param name="id">The identifier of the board to fetch.</param>
        /// <returns>A task whose result is the board's DTO, including its roots.</returns>
        /// <exception cref="BoardNotFoundException">No board exists with the given id.</exception>
        public async Task<BoardDto?> GetByIdAsync(BoardId id)
        {
            var board = await _repository.GetByIdAsync(id);

            if (board is null)
            {
                throw new BoardNotFoundException(id);
            }

            return _mapper.Map<BoardDto>(board);
        }

        /// <summary>
        /// Renames an existing board.
        /// </summary>
        /// <remarks>The board is loaded first so the rename is validated by the aggregate before the new
        /// title is written; a failed write is treated as the board having gone missing.</remarks>
        /// <param name="dto">The rename request carrying the board id and the new title.</param>
        /// <returns>A task whose result is true when the rename was persisted.</returns>
        /// <exception cref="BoardNotFoundException">No board exists with the given id, or the rename affected
        /// no rows.</exception>
        public async Task<bool> RenameAsync(RenameBoardDto dto)
        {
            var boardId = dto.Id;

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

        /// <summary>
        /// Adds an entity as a root of the given board.
        /// </summary>
        /// <param name="dto">The request identifying the board and the entity to add as a root.</param>
        /// <returns>A task whose result is true when the root was added.</returns>
        /// <exception cref="BoardNotFoundException">No board exists with the given id.</exception>
        /// <exception cref="RootAlreadyExistsException">The entity is already a root of the board.</exception>
        public async Task<bool> AddRootAsync(AddRootDto dto)
        {
            var boardId = dto.BoardId;
            var entityId = dto.EntityId;

            var boardExists = await _repository.BoardExistsAsync(boardId);

            if (!boardExists)
            {
                throw new BoardNotFoundException(boardId);
            }

            var rootExists = await _repository.RootExistsAsync(boardId, entityId);

            if (rootExists)
            {
                throw new RootAlreadyExistsException(entityId);
            }

            var added = await _repository.AddRootAsync(boardId, entityId);

            return added;
        }

        /// <summary>
        /// Removes an entity from the board's roots.
        /// </summary>
        /// <param name="dto">The request identifying the board and the entity to remove as a root.</param>
        /// <returns>A task whose result is true when the root was removed.</returns>
        /// <exception cref="BoardNotFoundException">No board exists with the given id.</exception>
        /// <exception cref="RootNotFoundException">The entity is not a root of the board.</exception>
        public async Task<bool> RemoveRootAsync(RemoveRootDto dto)
        {
            var boardId = dto.BoardId;
            var entityId = dto.EntityId;

            var boardExists = await _repository.BoardExistsAsync(boardId);

            if (!boardExists)
            {
                throw new BoardNotFoundException(boardId);
            }

            var rootExists = await _repository.RootExistsAsync(boardId, entityId);

            if (!rootExists)
            {
                throw new RootNotFoundException(entityId);
            }

            var removed = await _repository.RemoveRootAsync(boardId, entityId);

            return removed;
        }

        /// <summary>
        /// Deletes a board by its identifier.
        /// </summary>
        /// <param name="id">The identifier of the board to delete.</param>
        /// <returns>A task whose result is true when the board was deleted.</returns>
        /// <exception cref="BoardNotFoundException">No board exists with the given id, so nothing was
        /// deleted.</exception>
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
