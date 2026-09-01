namespace kanban_lia.Models.Domain.Columns.DTOs
{
    public record ColumnDto(
        Guid Id,
        string Title,
        int Position,
        Guid BoardId
    );
}
