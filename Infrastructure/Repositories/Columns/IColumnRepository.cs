using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Infrastructure.Repositories.Columns
{
    /// <summary>
    /// Provides data access for board columns.
    /// </summary>
    /// <remarks>Implementations are backed by the Columns table; lookups by id return null (or a null-able
    /// result) when no column matches, while update and delete methods report whether a row was affected.
    /// Deleting a column also removes the column edges that reference it.</remarks>
    public interface IColumnRepository
    {
        /// <summary>
        /// Persists a new column and returns it unchanged.
        /// </summary>
        /// <param name="column">The column aggregate to insert, including its board id, title, position, and
        /// request-writable flag.</param>
        /// <returns>A task whose result is the column that was stored.</returns>
        Task<Column> CreateAsync(Column column);

        /// <summary>
        /// Retrieves all columns of a board, ordered by their position.
        /// </summary>
        /// <param name="boardId">The board whose columns are fetched.</param>
        /// <returns>A task whose result is the board's columns in ascending position order; empty when the
        /// board has no columns.</returns>
        Task<IEnumerable<Column>> GetByBoardIdAsync(BoardId boardId);

        /// <summary>
        /// Retrieves a single column by its identifier.
        /// </summary>
        /// <param name="id">The identifier of the column to fetch.</param>
        /// <returns>A task whose result is the column, or null when no column has the given id.</returns>
        Task<Column?> GetByIdAsync(ColumnId id);

        /// <summary>
        /// Updates the request-writable flag that controls whether placements may be written to the column.
        /// </summary>
        /// <param name="id">The identifier of the column to update.</param>
        /// <param name="requestWritable">The new value of the request-writable flag.</param>
        /// <returns>A task whose result is true when a column row was updated, false when no column has the
        /// given id.</returns>
        Task<bool> SetRequestWritableAsync(ColumnId id, bool requestWritable);

        /// <summary>
        /// Reads the current request-writable flag of a column.
        /// </summary>
        /// <param name="id">The identifier of the column to inspect.</param>
        /// <returns>A task whose result is the column's request-writable flag, or null when no column has the
        /// given id.</returns>
        Task<bool?> CanBeWrittenAsync(ColumnId id);

        /// <summary>
        /// Updates the title of an existing column.
        /// </summary>
        /// <param name="id">The identifier of the column to rename.</param>
        /// <param name="title">The new title for the column.</param>
        /// <returns>A task whose result is true when a column row was updated, false when no column has the
        /// given id.</returns>
        Task<bool> RenameAsync(ColumnId id, string title);

        //Kanske ha en move method också??

        /// <summary>
        /// Deletes a column together with the edges that reference it.
        /// </summary>
        /// <remarks>Column edges whose from or to side equals the deleted column are removed first, so the
        /// edge table never retains dangling references.</remarks>
        /// <param name="id">The identifier of the column to delete.</param>
        /// <returns>A task whose result is true when rows were deleted, false when no column has the given
        /// id.</returns>
        Task<bool> DeleteAsync(ColumnId id);
    }
}