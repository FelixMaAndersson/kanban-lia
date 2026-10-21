using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Boards.DTOs;
using kanban_lia.Services.Boards.DTOs;

namespace kanban_lia.Services.Boards
{
    /// <summary>
    /// Application service that creates, reads, renames, roots and deletes boards.
    /// </summary>
    /// <remarks>Implementations validate titles through the <see cref="Board"/> domain type before creating or
    /// renaming a board. Missing, duplicate or invalid state is reported by throwing rather than by returning sentinel
    /// values, so the global exception handler can turn it into an appropriate problem response; the boolean results
    /// are <see langword="true"/> whenever the operation completed.</remarks>
    public interface IBoardService
    {
        /// <summary>
        /// Creates a board with the specified title and returns the identifier assigned to it.
        /// </summary>
        /// <param name="title">Title of the new board; must not be empty or whitespace and is at most 255 characters
        /// long.</param>
        /// <returns>A task containing the identifier of the newly created board.</returns>
        /// <exception cref="kanban_lia.Models.Domain.Exceptions.InvalidDomainException">
        /// The title is empty, whitespace only, or longer than 255 characters.
        /// </exception>
        Task<BoardId> CreateAsync(string title);
        /// <summary>
        /// Retrieves a board and maps it to its DTO representation.
        /// </summary>
        /// <param name="id">Identifier of the board to retrieve.</param>
        /// <returns>A task containing the DTO of the board; a missing board is reported as an exception rather than as
        /// <see langword="null"/>.</returns>
        /// <exception cref="kanban_lia.Services.Boards.Exceptions.BoardNotFoundException">
        /// No board with the specified identifier exists.
        /// </exception>
        Task<BoardDto?> GetByIdAsync(BoardId id);
        /// <summary>
        /// Renames the board identified by the DTO to the new title.
        /// </summary>
        /// <param name="dto">Holds the identifier of the board to rename and its new title, which must be non-empty
        /// and at most 255 characters long.</param>
        /// <returns>A task that is <see langword="true"/> when the new title was persisted.</returns>
        /// <exception cref="kanban_lia.Services.Boards.Exceptions.BoardNotFoundException">
        /// No board with the specified identifier exists, or it disappeared before the rename could be persisted.
        /// </exception>
        /// <exception cref="kanban_lia.Models.Domain.Exceptions.InvalidDomainException">
        /// The new title is empty, whitespace only, or longer than 255 characters.
        /// </exception>
        Task<bool> RenameAsync(RenameBoardDto dto);
        /// <summary>
        /// Registers an entity as a root of the board.
        /// </summary>
        /// <param name="dto">Holds the board to add the root to and the identifier of the entity to add.</param>
        /// <returns>A task that is <see langword="true"/> when the root was added.</returns>
        /// <exception cref="kanban_lia.Services.Boards.Exceptions.BoardNotFoundException">
        /// No board with the specified identifier exists.
        /// </exception>
        /// <exception cref="kanban_lia.Services.Boards.Exceptions.RootAlreadyExistsException">
        /// The entity is already a root of the board.
        /// </exception>
        Task<bool> AddRootAsync(AddRootDto dto);
        /// <summary>
        /// Removes a root entity from the board.
        /// </summary>
        /// <param name="dto">Holds the board to remove the root from and the identifier of the entity to remove.</param>
        /// <returns>A task that is <see langword="true"/> when the root was removed.</returns>
        /// <exception cref="kanban_lia.Services.Boards.Exceptions.BoardNotFoundException">
        /// No board with the specified identifier exists.
        /// </exception>
        /// <exception cref="kanban_lia.Services.Boards.Exceptions.RootNotFoundException">
        /// The entity is not a root of the board.
        /// </exception>
        Task<bool> RemoveRootAsync(RemoveRootDto dto);
        /// <summary>
        /// Deletes the board with the specified identifier.
        /// </summary>
        /// <param name="id">Identifier of the board to delete.</param>
        /// <returns>A task that is <see langword="true"/> when the board was deleted.</returns>
        /// <exception cref="kanban_lia.Services.Boards.Exceptions.BoardNotFoundException">
        /// No board with the specified identifier existed, so nothing was deleted.
        /// </exception>
        Task<bool> DeleteAsync(BoardId id);
    }
}
