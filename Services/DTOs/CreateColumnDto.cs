namespace kanban_lia.Services.DTOs
{
    public record CreateColumnDto(string Title, int Position, Guid BoardId);
}
