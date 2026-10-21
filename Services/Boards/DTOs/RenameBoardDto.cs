using kanban_lia.Models.Domain.Boards;

namespace kanban_lia.Services.Boards.DTOs
{
    /// <summary>
    /// Service-level request to rename a board, carrying the domain-typed board identifier.
    /// </summary>
    /// <remarks>Mapped from the <see cref="kanban_lia.Endpoints.Boards.Requests.RenameBoardRequest"/> endpoint payload
    /// and validated by the <see cref="Board"/> aggregate before the new title is persisted.</remarks>
    /// <param name="Id">The identifier of the board to rename.</param>
    /// <param name="NewTitle">The replacement title; must be non-empty and at most 255 characters.</param>
    public record RenameBoardDto(
        BoardId Id,
        string NewTitle);
}
