using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Infrastructure.Repositories.Columns
{
    /// <summary>
    /// Provides storage for column edges, the directed links that describe how columns are connected within and across
    /// boards.
    /// </summary>
    /// <remarks>Every method opens its own connection and runs a single statement, so no caller-managed transaction is
    /// involved. Existence checks and deletion are direction-agnostic: an edge stored as (A to B) also matches a lookup
    /// for (B to A). Database failures such as constraint violations propagate unchanged as
    /// <see cref="Microsoft.Data.SqlClient.SqlException"/>.</remarks>
    public interface IColumnEdgeRepository
    {
        /// <summary>
        /// Inserts a new directed edge between two columns.
        /// </summary>
        /// <param name="columnEdge">The edge to persist; both endpoint columns must already exist.</param>
        /// <returns>A task that completes when the row has been inserted.</returns>
        /// <exception cref="Microsoft.Data.SqlClient.SqlException">
        /// The insert violates a database constraint, for example because one of the endpoint columns does not exist.
        /// </exception>
        Task CreateAsync(ColumnEdge columnEdge);
        /// <summary>
        /// Determines whether the edge already exists, in either direction.
        /// </summary>
        /// <param name="columnEdge">The edge to look for; the reverse orientation counts as a match.</param>
        /// <returns>A task that is <see langword="true"/> when at least one matching row exists; otherwise
        /// <see langword="false"/>.</returns>
        Task<bool> ExistsAsync(ColumnEdge columnEdge);
        /// <summary>
        /// Gets every edge whose source or target column belongs to the specified board.
        /// </summary>
        /// <param name="boardId">Board whose columns are used to match edge endpoints.</param>
        /// <returns>A task containing the matching edges; empty when the board has no connected columns.</returns>
        Task<IEnumerable<ColumnEdge>> GetByBoardIdAsync(BoardId boardId);
        /// <summary>
        /// Gets all edges that start at the specified column.
        /// </summary>
        /// <param name="fromColumnId">Column whose outgoing edges are wanted.</param>
        /// <returns>A task containing the outgoing edges; empty when the column has none.</returns>
        Task<IEnumerable<ColumnEdge>> GetByFromColumnIdAsync(ColumnId fromColumnId);
        /// <summary>
        /// Gets all edges that point to the specified column.
        /// </summary>
        /// <param name="toColumnId">Column whose incoming edges are wanted.</param>
        /// <returns>A task containing the incoming edges; empty when the column has none.</returns>
        Task<IEnumerable<ColumnEdge>> GetByToColumnIdAsync(ColumnId toColumnId);
        /// <summary>
        /// Deletes the edge, matching the stored direction and its reverse.
        /// </summary>
        /// <param name="columnEdge">The edge to remove.</param>
        /// <returns>A task that is <see langword="true"/> when a row was deleted; <see langword="false"/> when no such
        /// edge existed.</returns>
        Task<bool> DeleteAsync(ColumnEdge columnEdge);
    }
}
