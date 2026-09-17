namespace kanban_lia.Models.Domain.Columns.DTOs
{
    public record ColumnEdgeDto(
        ColumnId FromColumnId,
        ColumnId ToColumnId
    );
}
