using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Columns.DTOs;
using kanban_lia.Services.Columns.DTOs;
using kanban_lia.Services.IntegrationEvents.Models;

namespace kanban_lia.Services.Columns
{
    /// <summary>
    /// Service contract for managing the columns of a board.
    /// </summary>
    /// <remarks>Implementations throw a not-found exception when an operation targets a board or column that does
    /// not exist, so callers can rely on exceptions rather than null checks to detect missing entities.</remarks>
    public interface IColumnService
    {
        /// <summary>
        /// Creates a new column on a board from the given specification.
        /// </summary>
        /// <param name="dto">The column specification: optional pre-set id, board id, title, position, and the
        /// request-writable flag.</param>
        /// <param name="correlationId">Correlation identifier of the request.</param>
        /// <param name="actor">Actor performing the creation.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>A task whose result is the identifier of the newly created column.</returns>
        Task<ColumnId> CreateAsync(CreateColumnDto dto, Guid correlationId, Actor actor, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the columns of a board, mapped to DTOs.
        /// </summary>
        /// <param name="boardId">Identifier of the board whose columns are requested.</param>
        /// <returns>A task whose result is the board's columns as <see cref="ColumnDto"/> instances.</returns>
        Task<IEnumerable<ColumnDto>> GetByBoardIdAsync(BoardId boardId);

        /// <summary>
        /// Returns a single column by its identifier.
        /// </summary>
        /// <param name="id">Identifier of the column to retrieve.</param>
        /// <returns>A task whose result is the column mapped to a <see cref="ColumnDto"/>.</returns>
        Task<ColumnDto?> GetByIdAsync(ColumnId id);

        /// <summary>
        /// Sets whether requests may write to the column.
        /// </summary>
        /// <param name="id">Identifier of the column to update.</param>
        /// <param name="requestWritable">The new value of the request-writable flag.</param>
        /// <returns>A task whose result is <see langword="true"/> once the flag has been updated.</returns>
        Task<bool> SetRequestWritableAsync(ColumnId id, bool requestWritable);

        /// <summary>
        /// Renames a column after validating the new title against the domain rules.
        /// </summary>
        /// <param name="dto">The rename instruction carrying the column id and the new title.</param>
        /// <returns>A task whose result is <see langword="true"/> once the column has been renamed.</returns>
        Task<bool> RenameAsync(RenameColumnDto dto);

        /// <summary>
        /// Deletes a column by its identifier.
        /// </summary>
        /// <param name="id">Identifier of the column to delete.</param>
        /// <returns>A task whose result is <see langword="true"/> once the column has been deleted.</returns>
        Task<bool> DeleteAsync(ColumnId id);
    }
}