using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Columns.DTOs;
using kanban_lia.Services.Columns.DTOs;

namespace kanban_lia.Services.Columns
{
    /// <summary>
    /// Provides operations for managing the edges that connect board columns.
    /// </summary>
    /// <remarks>Callers read this interface rather than the implementation; edge direction is normalised on
    /// creation so a pair of connected columns is stored only once.</remarks>
    public interface IColumnEdgeService
    {
        /// <summary>
        /// Creates an edge between two columns, rejecting duplicates.
        /// </summary>
        /// <param name="dto">The edge request identifying the two columns to connect; the ids may be supplied
        /// in either order.</param>
        /// <returns>A task that completes when the edge has been stored.</returns>
        Task CreateAsync(CreateColumnEdgeDto dto);

        /// <summary>
        /// Retrieves every edge that connects two columns of the given board.
        /// </summary>
        /// <param name="boardId">The board whose edges are fetched.</param>
        /// <returns>A task whose result is the board's column edges.</returns>
        Task<IEnumerable<ColumnEdgeDto>> GetByBoardIdAsync(BoardId boardId);

        /// <summary>
        /// Retrieves the edges that start at the given column.
        /// </summary>
        /// <param name="fromColumnId">The column whose outgoing edges are fetched.</param>
        /// <returns>A task whose result is the outgoing edges; empty when the column has none.</returns>
        Task<IEnumerable<ColumnEdgeDto>> GetByFromColumnIdAsync(ColumnId fromColumnId);

        /// <summary>
        /// Retrieves the edges that end at the given column.
        /// </summary>
        /// <param name="toColumnId">The column whose incoming edges are fetched.</param>
        /// <returns>A task whose result is the incoming edges; empty when the column has none.</returns>
        Task<IEnumerable<ColumnEdgeDto>> GetByToColumnIdAsync(ColumnId toColumnId);

        /// <summary>
        /// Deletes the edge between the two columns identified by the given edge.
        /// </summary>
        /// <param name="edge">The edge to delete; both column ids must match a stored edge.</param>
        /// <returns>A task whose result is true when an edge was deleted, false when no matching edge
        /// exists.</returns>
        Task<bool> DeleteAsync(ColumnEdge edge);
    }
}
