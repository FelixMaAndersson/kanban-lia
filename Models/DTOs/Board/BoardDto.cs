namespace kanban_lia.Models.DTOs.Board
{
    public record BoardDto(
        Guid Id,
        string Title,
        IReadOnlyCollection<Guid> Roots
    );
}
