namespace kanban_lia.Models.Domain.Columns.DTOs
{
    /// <summary>
    /// Read model representing a directed edge between two columns on a board.
    /// </summary>
    /// <remarks>Edges express which columns are connected for the purposes of cross-board placement; the direction
    /// runs from the source column to the target column.</remarks>
    /// <param name="FromColumnId">Identifier of the column the edge starts from.</param>
    /// <param name="ToColumnId">Identifier of the column the edge points to.</param>
    public record ColumnEdgeDto(
        ColumnId FromColumnId,
        ColumnId ToColumnId
    );
}
