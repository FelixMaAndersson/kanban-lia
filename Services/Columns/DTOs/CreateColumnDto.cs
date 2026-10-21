using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Services.Columns.DTOs

{
    /// <summary>
    /// DTO carrying the data needed to create a column on a board.
    /// </summary>
    /// <remarks>Passed to the column service, which forwards it to <c>Column.Create</c>; that factory rejects empty
    /// titles and titles longer than 255 characters, and negative positions, by throwing an
    /// <see cref="kanban_lia.Models.Domain.Exceptions.InvalidDomainException"/>.</remarks>
    /// <param name="Id">The column's ID, or null to have one generated when the column is created.</param>
    /// <param name="Title">The column title; must be non-empty and at most 255 characters.</param>
    /// <param name="Position">The column's position within its board; must not be negative.</param>
    /// <param name="BoardId">The board the column belongs to.</param>
    /// <param name="RequestWritable">Whether placement requests are allowed to write into the column.</param>
    public record CreateColumnDto(
        ColumnId? Id,
        string Title,
        int Position,
        BoardId BoardId,
        bool RequestWritable);
}
