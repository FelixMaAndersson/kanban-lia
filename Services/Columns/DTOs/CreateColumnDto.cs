namespace kanban_lia.Services.Columns.DTOs
{
    public record CreateColumnDto(string Title, int Position, Guid BoardId);
}
