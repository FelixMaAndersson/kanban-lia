namespace kanban_lia.Models.DTOs.Column
{
    public record ColumnDto(
        Guid Id,
        string Title,
        int Position,
        int BoardId
    );
}
