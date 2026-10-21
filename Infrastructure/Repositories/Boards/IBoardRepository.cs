using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Infrastructure.Repositories.Boards
{
    /// <summary>
    /// Provides persistence for boards and the root entities registered on them.
    /// </summary>
    /// <remarks>Boards are stored in the Boards table and roots in BoardRoots, which maps a board to the entities
    /// that start on it; <see cref="GetByIdAsync"/> rehydrates both into a <see cref="Board"/>. Mutating members
    /// report the outcome as a Boolean instead of throwing when the target row is simply absent, so callers decide
    /// whether a false result is an error. Each call opens its own connection; none of the members share a
    /// transaction.</remarks>
    public interface IBoardRepository
    {
        /// <summary>
        /// Inserts a new board with its title.
        /// </summary>
        /// <param name="board">The board to persist; its own <see cref="Board.Id"/> and current
        /// <see cref="Board.Title"/> are written.</param>
        /// <returns>A task whose result is the identifier the board was stored under.</returns>
        Task<BoardId> CreateAsync(Board board);
        /// <summary>
        /// Loads a board together with its root entities.
        /// </summary>
        /// <remarks>The board's roots are added to the returned instance as they are read; the order in which they
        /// are enumerated is not defined.</remarks>
        /// <param name="id">Identifier of the board to load.</param>
        /// <returns>A task whose result is the board, or null when no board exists with that identifier.</returns>
        Task<Board?> GetByIdAsync(BoardId id);
        /// <summary>
        /// Updates the title of an existing board.
        /// </summary>
        /// <param name="id">Identifier of the board to rename.</param>
        /// <param name="title">The replacement title.</param>
        /// <returns>true when a row was updated; false when no board exists with that identifier.</returns>
        Task<bool> RenameAsync(BoardId id, string title);
        /// <summary>
        /// Registers an entity as a root of the board.
        /// </summary>
        /// <remarks>The pair is unique: because BoardRoots has a primary key on (BoardId, EntityId) and a foreign
        /// key to Boards, a duplicate root or an unknown board fails as a SQL constraint violation rather than
        /// returning false; callers typically check <see cref="RootExistsAsync"/> first to report that case
        /// themselves.</remarks>
        /// <param name="id">Identifier of the board that gains the root.</param>
        /// <param name="entityId">Identifier of the entity to add as a root.</param>
        /// <returns>true when the root row was inserted; false when no row was affected.</returns>
        Task<bool> AddRootAsync(BoardId id, EntityId entityId);
        /// <summary>
        /// Removes a root entity from the board.
        /// </summary>
        /// <param name="id">Identifier of the board that loses the root.</param>
        /// <param name="entityId">Identifier of the root to remove.</param>
        /// <returns>true when a row was deleted; false when the board has no such root.</returns>
        Task<bool> RemoveRootAsync(BoardId id, EntityId entityId);
        /// <summary>
        /// Deletes a board and the column edges that point in or out of its columns.
        /// </summary>
        /// <remarks>The column edges are removed before the board row itself, in a single command. The reported
        /// result covers the whole command, so it is true when any of those rows were deleted - including when only
        /// edges were removed because the board row was already gone. Roots are removed automatically by the
        /// database's cascade delete.</remarks>
        /// <param name="id">Identifier of the board to delete.</param>
        /// <returns>true when at least one row was deleted; false when nothing matched.</returns>
        Task<bool> DeleteAsync(BoardId id);
        /// <summary>
        /// Checks whether a board with the given identifier exists.
        /// </summary>
        /// <param name="id">Identifier to look up.</param>
        /// <returns>true when a board row exists; false otherwise.</returns>
        Task<bool> BoardExistsAsync(BoardId id);
        /// <summary>
        /// Checks whether the given entity is registered as a root of the given board.
        /// </summary>
        /// <param name="id">Identifier of the board to check.</param>
        /// <param name="entityId">Identifier of the entity to look for.</param>
        /// <returns>true when the root is registered; false otherwise.</returns>
        Task<bool> RootExistsAsync(BoardId id, EntityId entityId);
    }
}
