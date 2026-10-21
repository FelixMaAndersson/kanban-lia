using System.Data;

using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Infrastructure.Repositories.Placements
{
    /// <summary>
    /// Represents the sort keys surrounding a resolved position within a column.
    /// </summary>
    /// <param name="Previous">Sort key of the placement immediately before the resolved position, or
    /// <see langword="null"/> when the position is at the start of the column or no predecessor exists.</param>
    /// <param name="Next">Sort key of the placement immediately after the resolved position, or
    /// <see langword="null"/> when the position is at the end of the column or no successor exists.</param>
    public record SortKeyRange(string? Previous, string? Next);
    /// <summary>
    /// Selects how the target position for a placement is resolved when looking up a sort-key range.
    /// </summary>
    public enum SortKeyLookup
    {
        Last = 0,
        After = 1,
        Before = 2
    }
    /// <summary>
    /// Stores placement history and reads the current placement of each entity.
    /// </summary>
    /// <remarks>Placements are append-only: every move inserts a new row, and the current placement of an entity is
    /// the row with the latest timestamp for that entity on the board. Concurrent moves of the same entity to
    /// different columns can share a timestamp; no tie-breaker is applied, so either may be treated as current. Sort
    /// keys are compared with binary collation when ordering within a column.</remarks>
    public interface IPlacementRepository
    {
        /// <summary>
        /// Acquires an exclusive application lock protecting sort-key calculations for the given column within the
        /// given transaction.
        /// </summary>
        /// <param name="transaction">The transaction that owns the lock; the lock is released when the transaction
        /// commits or rolls back.</param>
        /// <param name="columnId">Identifier of the column whose sort keys are protected.</param>
        /// <param name="cancellationToken">A token used to cancel the lock request.</param>
        /// <returns>A task that completes when the lock has been acquired.</returns>
        /// <exception cref="InvalidOperationException">The lock could not be acquired within the timeout.</exception>
        Task AcquireSortKeyLockAsync(
            IDbTransaction transaction,
            ColumnId columnId,
            CancellationToken cancellationToken);
        /// <summary>
        /// Inserts the given placements within the given transaction.
        /// </summary>
        /// <param name="placements">The placement rows to insert.</param>
        /// <param name="transaction">The transaction that scopes the insert.</param>
        /// <returns>A task that completes when the rows have been inserted.</returns>
        Task CreateAsync(IEnumerable<Placement> placements, IDbTransaction transaction);
        /// <summary>
        /// Gets the current placement of each of the given entities on the given board.
        /// </summary>
        /// <param name="entityIds">Identifiers of the entities whose current placements are requested.</param>
        /// <param name="boardId">Identifier of the board to restrict the search to.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the current
        /// placement of each requested entity that has one on the board.</returns>
        Task<IEnumerable<Placement>> GetCurrentAsync(IEnumerable<EntityId> entityIds, BoardId boardId);
        /// <summary>
        /// Gets the current placement of each of the given entities on the given board within the given transaction.
        /// </summary>
        /// <param name="entityIds">Identifiers of the entities whose current placements are requested.</param>
        /// <param name="boardId">Identifier of the board to restrict the search to.</param>
        /// <param name="transaction">The transaction used for the query.</param>
        /// <param name="cancellationToken">A token used to cancel the query.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the current
        /// placement of each requested entity that has one on the board.</returns>
        Task<IEnumerable<Placement>> GetCurrentAsync(
            IEnumerable<EntityId> entityIds,
            BoardId boardId,
            IDbTransaction transaction,
            CancellationToken cancellationToken);
        /// <summary>
        /// Gets the current placement of every entity on the given board, ordered by column and sort key.
        /// </summary>
        /// <param name="boardId">Identifier of the board whose placements are requested.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the current
        /// placements for the board.</returns>
        Task<IEnumerable<Placement>> GetCurrentByBoardAsync(BoardId boardId);
        /// <summary>
        /// Gets the current placements of the entities currently in the given column, ordered by sort key.
        /// </summary>
        /// <param name="columnId">Identifier of the column whose placements are requested.</param>
        /// <param name="boardId">Identifier of the board the column belongs to.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the current
        /// placements in the column.</returns>
        Task<IEnumerable<Placement>> GetCurrentByColumnAsync(ColumnId columnId, BoardId boardId);
        /// <summary>
        /// Looks up the sort keys surrounding a target position within a column so a new or moved placement can be
        /// inserted between them.
        /// </summary>
        /// <param name="transaction">The transaction used for the query; the sort-key lock for the column is expected
        /// to be held by the same transaction.</param>
        /// <param name="columnId">Identifier of the column being evaluated.</param>
        /// <param name="lookup">Selects whether the range is at the end of the column or relative to a specified
        /// entity.</param>
        /// <param name="afterEntityId">Entity whose current placement the range should follow; used when
        /// <paramref name="lookup"/> is <see cref="SortKeyLookup.After"/>.</param>
        /// <param name="beforeEntityId">Entity whose current placement the range should precede; used when
        /// <paramref name="lookup"/> is <see cref="SortKeyLookup.Before"/>.</param>
        /// <param name="cancellationToken">A token used to cancel the query.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the previous and
        /// next sort keys for the resolved position.</returns>
        Task<SortKeyRange> GetSortKeyRangeAsync(
            IDbTransaction transaction,
            ColumnId columnId,
            SortKeyLookup lookup,
            EntityId? afterEntityId = null,
            EntityId? beforeEntityId = null,
            CancellationToken cancellationToken = default);
    }
}
