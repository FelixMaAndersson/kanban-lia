namespace kanban_lia.Endpoints.Boards.Requests
{
    /// <summary>
    /// Request payload for renaming an existing board.
    /// </summary>
    /// <remarks>Bound from the request body of the PUT /api/boards/rename endpoint and mapped to a
    /// <see cref="kanban_lia.Services.Boards.DTOs.RenameBoardDto"/> before being passed to the board service. The new
    /// title is validated by the <see cref="kanban_lia.Models.Domain.Boards.Board"/> aggregate, and the service throws
    /// when no board with the given id exists.</remarks>
    /// <param name="Id">The unique identifier of the board to rename.</param>
    /// <param name="NewTitle">The replacement title for the board; must be non-empty and at most 255 characters.</param>
    public record RenameBoardRequest(
        Guid Id,
        string NewTitle);
}
