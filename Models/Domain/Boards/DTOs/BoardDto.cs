namespace kanban_lia.Models.Domain.Boards.DTOs
{
    public record BoardDto(
        Guid Id,
        string Title,
        IReadOnlyCollection<Guid> Roots
    );
}
