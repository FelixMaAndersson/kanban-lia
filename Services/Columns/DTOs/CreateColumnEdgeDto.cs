using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Services.Columns.DTOs
{
    /// <summary>
    /// Service-level request to create an edge connecting two columns.
    /// </summary>
    /// <remarks>Mapped from the column-edge endpoint payload. The service normalises the two ids before storing, so a
    /// pair of columns is stored exactly once regardless of the order they are supplied in; self-edges are rejected.
    /// Edges determine which columns a placement may move between.</remarks>
    /// <param name="FromColumnId">One of the two columns to connect; may be the lower- or higher-ordered id of the
    /// pair.</param>
    /// <param name="ToColumnId">The other column to connect; must differ from <paramref name="FromColumnId"/>.</param>
    public record CreateColumnEdgeDto(
        ColumnId FromColumnId,
        ColumnId ToColumnId);
}
