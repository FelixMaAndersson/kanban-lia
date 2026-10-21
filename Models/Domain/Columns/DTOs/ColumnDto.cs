using kanban_lia.Models.Domain.Boards;

namespace kanban_lia.Models.Domain.Columns.DTOs
{
    /// <summary>
    /// Data-transfer representation of a board column.
    /// </summary>
    /// <param name="Id">The column's unique identifier.</param>
    /// <param name="BoardId">The board the column belongs to.</param>
    /// <param name="Title">The display title of the column.</param>
    /// <param name="Position">The column's left-to-right ordering position within its board.</param>
    /// <param name="RequestWritable">Whether placements may currently be written to the column; false while
    /// the column is locked for requests.</param>
    public record ColumnDto(
        ColumnId Id,
        BoardId BoardId,
        string Title,
        int Position,
        bool RequestWritable
    );
}
